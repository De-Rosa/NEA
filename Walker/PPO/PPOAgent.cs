using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using Physics.Rendering;
using Physics.Walker.PPO.Network;

namespace Physics.Walker.PPO;

public partial class PPOAgent
{
    private readonly NeuralNetwork _criticNetwork;
    private readonly NeuralNetwork _actorNetwork;
    private float _logStandardDeviation = -0.5f;

    // Agent hyper-parameters
    private const int DenseSize = 128;
    private const int Epochs = 5;
    private const int BatchSize = 512;
    private const float Gamma = 0.99f; // Discount Factor
    private const float Lambda = 0.95f; // Smoothing Factor
    private const float Epsilon = 0.2f; // Clipping Factor

    private readonly int StateSize;
    private readonly int ActionSize;

    public PPOAgent(int stateSize, int actionSize)
    {
        StateSize = stateSize;
        ActionSize = actionSize;
        
        // Critic neural network transforms a state into a scalar value estimate for use in training the actor
        _criticNetwork = new NeuralNetwork();
        _criticNetwork.AddLayer(new DenseLayer(stateSize, DenseSize, BatchSize));
        _criticNetwork.AddLayer(new LeakyReLULayer());
        _criticNetwork.AddLayer(new DenseLayer(DenseSize, 1, BatchSize));

        // Actor mean neural network transforms a state into an array of means to use in action sampling.
        _actorNetwork = new NeuralNetwork();
        _actorNetwork.AddLayer(new DenseLayer(stateSize, DenseSize, BatchSize));
        _actorNetwork.AddLayer(new LeakyReLULayer());
        _actorNetwork.AddLayer(new DenseLayer(DenseSize, actionSize, BatchSize));
        _actorNetwork.AddLayer(new TanhLayer());
    }

    public void Save(string criticFileLocation, string muFileLocation)
    {
        string[] criticNetwork = _criticNetwork.Save();
        string[] muActorNetwork = _actorNetwork.Save();

        File.WriteAllLines(criticFileLocation, criticNetwork);
        File.WriteAllLines(muFileLocation, muActorNetwork);

    }

    public void Load(string criticFileLocation, string muFileLocation)
    {
        string[] criticNetwork = File.ReadAllLines(criticFileLocation);
        string[] muNetwork = File.ReadAllLines(muFileLocation);

        if (!File.Exists(criticFileLocation) || !File.Exists(muFileLocation))
        {
            Console.WriteLine("Cannot load weights, the weights files do not exist.");
            return;
        }
        
        _criticNetwork.Load(criticNetwork);
        _actorNetwork.Load(muNetwork);
    }

    public void Render(Renderer renderer)
    {
        _actorNetwork.Render(renderer);
    }

    public void Train(Trajectory trajectory)
    {
        for (int i = 0; i < Epochs; i++)
        {
            List<Batch> batches = CreateBatches(trajectory);
            
            for (int j = 0; j < batches.Count; j++)
            {
                CalculateValueEstimates(trajectory);
                MonteCarloReturn(trajectory);
                MonteCarloAdvantages(trajectory);
                UpdateBatches(batches, trajectory);
                
                Train(batches[j], out float valueLoss);
                Console.WriteLine($"Epoch {i + 1}/{Epochs} | Batch: {j + 1}/{batches.Count} | Value loss: {valueLoss}");
            }
        }
        
        Console.WriteLine($"New standard deviation: {MathF.Exp(_logStandardDeviation - 0.001f)}, previously {Math.Exp(_logStandardDeviation)}");
        _logStandardDeviation -= 0.001f;
    }

    private void Train(Batch batch, out float totalCriticLoss)
    {
        _actorNetwork.Zero();
        _criticNetwork.Zero();

        totalCriticLoss = 0;
        
        Matrix std = GetStandardDeviations();

        // https://fse.studenttheses.ub.rug.nl/25709/1/mAI_2021_BickD.pdf
        // gradient accumulation
        for (int i = 0; i < BatchSize; i++)
        {
            // Derivative of the mean squared error
            // -2(G - V(s))
            float criticLoss = 2 * (batch.Values[i] - batch.Returns[i]);
            
            Matrix mean = GetMeanOutput(batch.States[i]);
            Matrix logProbabilities = GetLogProbabilities(mean, std, batch.Actions[i]);
            
            // Derivative of L Clip with respect to the policy
            // Equation 20
            Matrix ratio = Matrix.Exponential(logProbabilities - batch.LogProbabilities[i]);
            
            Matrix clippedRatio = Matrix.Clip(ratio, 1f + Epsilon, 1f - Epsilon);
            Matrix clippedRatioAdvantage = clippedRatio * batch.Advantages[i];
            Matrix ratioAdvantage = ratio * batch.Advantages[i];

            Matrix partA = Matrix.Compare(ratioAdvantage, clippedRatioAdvantage, 1f, 0f);
            partA *= batch.Advantages[i];

            Matrix partB = Matrix.CompareNonEquals(clippedRatioAdvantage, ratioAdvantage, 1f, 0f);
            partB *= batch.Advantages[i];

            Matrix partC = Matrix.CompareInRange(ratio, 1f + Epsilon, 1f - Epsilon, 1f, 0f);

            Matrix lClipDerivative = partA + Matrix.HadamardProduct(partB, partC);
            lClipDerivative *= -1f;
            lClipDerivative = Matrix.HadamardDivision(lClipDerivative, Matrix.Exponential(batch.LogProbabilities[i]));
            
            // Derivative of mean with respect to the policy
            // Equation 26
            Matrix probabilities = Matrix.Exponential(logProbabilities);
            
            Matrix actionsMinusMean = batch.Actions[i] - mean;
            Matrix variance = Matrix.HadamardProduct(std, std);

            Matrix meanDerivative = Matrix.HadamardProduct(probabilities,  (Matrix.HadamardDivision(actionsMinusMean, variance)));
            meanDerivative = Matrix.HadamardProduct(meanDerivative, lClipDerivative);
            
            criticLoss /= BatchSize;
            meanDerivative /= BatchSize;
            
            _criticNetwork.FeedBack(Matrix.FromValues(new float[] { criticLoss }));
            _actorNetwork.FeedBack(meanDerivative);

            totalCriticLoss += criticLoss;
        }
        
        _criticNetwork.Optimise();
        _actorNetwork.Optimise();
    }

    // We calculate V(s) which is called the value function. This is the discounted returns if the AI behaves as expected, and does not take
    // into account the randomness of taking actions
    private float GetValueEstimate(Matrix state)
    {
        return _criticNetwork.FeedForward(state).GetValue(0,0);
    }

    private Matrix GetStandardDeviations()
    {
        Matrix std = Matrix.FromSize(ActionSize, 1);
        float stdValue = MathF.Exp(_logStandardDeviation);
        
        for (int i = 0; i < ActionSize; i++)
        {
            std.SetValue(i, 0, stdValue);
        }

        return std;
    }

    public Matrix SampleActions(Matrix state, out Matrix logProbabilities, out Matrix mean, out Matrix std)
    {
        mean = GetMeanOutput(state);
        std = GetStandardDeviations();
        
        Matrix actions = Matrix.SampleNormal(mean, std);
        logProbabilities = GetLogProbabilities(mean, std, actions);

        return actions;
    }

    private static Matrix GetLogProbabilities(Matrix mean, Matrix std, Matrix actions)
    {
        return Matrix.LogNormalDensities(mean, std, actions);
    }

    private Matrix GetMeanOutput(Matrix state)
    {
        return _actorNetwork.FeedForward(state);
    }

    // apparently bootstrapping values does not work practically on single workers
    private void GeneralizedAdvantageEstimate(Trajectory trajectory)
    {
        float nextGae = 0;
        float nextValue = 0;
        
        for (int i = trajectory.States.Count - 1; i >= 0; i--)
        {
            float delta = CalculateDelta(trajectory, i, ref nextValue);
            float GAE = delta + (Gamma * Lambda * nextGae);
            trajectory.Advantages.Add(GAE);
            trajectory.Returns.Add(GAE);
        }
        
        trajectory.Advantages.Reverse();
        trajectory.Returns.Reverse();
    }

    private float CalculateDelta(Trajectory trajectory, int time, ref float nextValue)
    {
        float currentValue = trajectory.Values[time];
        float delta = trajectory.Rewards[time] + (Gamma * nextValue) - currentValue;
        nextValue = currentValue;

        return delta;
    }
    
    private void CalculateValueEstimates(Trajectory trajectory)
    {
        trajectory.Values.Clear();
        
        for (int i = 0; i < trajectory.States.Count; i++)
        {
            float value = GetValueEstimate(trajectory.States[i]);
            trajectory.Values.Add(value);
        }
    }

    // https://datascience.stackexchange.com/questions/20098/why-do-we-normalize-the-discounted-rewards-when-doing-policy-gradient-reinforcem
    // https://stackoverflow.com/questions/3141692/standard-deviation-of-generic-list
    private void Standardize(Trajectory trajectory, List<float> list)
    {
        if (list.Count == 0) return;
        float mean = list.Average();
        float std = (float) Math.Sqrt(list.Sum(value => Math.Pow(value - mean, 2)) / (list.Count));
        
        for (int i = 0; i < list.Count; i++)
        {
            list[i] -= mean;
            list[i] /= std;
        }
    }

    private void MonteCarloReturn(Trajectory trajectory)
    {
        float discountedReturns = 0;
        for (int i = trajectory.States.Count - 1; i >= 0; i--)
        {
            discountedReturns = trajectory.Rewards[i] + (discountedReturns * Gamma);
            trajectory.Returns.Add(discountedReturns);
        }

        trajectory.Returns.Reverse();
    }

    private void MonteCarloAdvantages(Trajectory trajectory)
    {
        trajectory.Advantages.Clear();
        
        for (int i = 0; i < trajectory.States.Count; i++)
        {
            trajectory.Advantages.Add(trajectory.Returns[i] - trajectory.Values[i]);
        }
    }
    
    private List<Batch> CreateBatches(Trajectory trajectory)
    {
        List<Batch> batches = new List<Batch>();
        Random random = new Random();
        Trajectory newTrajectory = trajectory.Copy();
        
        int batchCount = (newTrajectory.States.Count / BatchSize);
        
        for (int i = 0; i < batchCount; i++)
        {
            Batch batch = new Batch(BatchSize);
            
            for (int j = 0; j < BatchSize; j++)
            {
                int index = random.Next(0, newTrajectory.States.Count - 1);
                batch.Index = index;
                batch.States[j] = newTrajectory.States[index];
                batch.Actions[j] = newTrajectory.Actions[index];
                batch.Means[j] = newTrajectory.Means[index];
                batch.Stds[j] = newTrajectory.Stds[index];
                batch.LogProbabilities[j] = newTrajectory.LogProbabilities[index];
                batch.Rewards[j] = newTrajectory.Rewards[index];
                newTrajectory.States.RemoveAt(index);
            }
            batches.Add(batch);
        }

        return batches;
    }

    private void UpdateBatches(List<Batch> batches, Trajectory trajectory)
    {
        foreach (var batch in batches)
        {
            for (int j = 0; j < BatchSize; j++)
            {
                batch.Values[j] = trajectory.Values[batch.Index];
                batch.Returns[j] = trajectory.Returns[batch.Index];
                batch.Advantages[j] = trajectory.Advantages[batch.Index];
            }
        }
    }
    
}