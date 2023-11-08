using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NEA.Walker.PPO;
using NEA.Walker.PPO.Network;

namespace NEA.Rendering;

// Console rendering class, functions which display information inside the console.
// Since I cannot implement text inside of the GUI, we have to use the console to display information.
public class ConsoleRenderer
{
    private const string ConfigurationLocation = "Data/Configurations/";
    private const string DefaultConfigName = "config";
    private const string DataLocation = "Data/SavedRewards/";
    private const string DefaultDataName = "data";
    private const string WeightsLocation = "Data/Weights/";
    
    private List<float> _averageRewards;

    public ConsoleRenderer()
    {
        _averageRewards = new List<float>();
    }
    
    // Original update, opens the starting menu.
    public void Update()
    { 
        StartingMenu();
    }

    // Update during an episode, renders the current information.
    public void Update(int episode, int timeStep, float distance, float averageReward, float bestDistance, float pastAverageReward, Matrix state)
    {
        RolloutInformation(episode, timeStep, distance, averageReward, bestDistance, pastAverageReward, state);
    }

    // Update during training, renders the current training information.
    public void Update(int epoch, int batch, int batchSize, float criticLoss)
    {
        TrainingInformation(epoch, batch, batchSize, criticLoss);
    }

    // Displays the current episode information and the current state.
    private void RolloutInformation(int episode, int timeStep, float distance, float averageReward, float bestDistance, float pastAverageReward, Matrix state)
    {
        Console.Clear();

        DrawBorder();
        Console.WriteLine($"Episode {episode}, timestep {timeStep} ({((float) (timeStep) / Hyperparameters.MaxTimesteps) * 100f} % of max timesteps)");
        DrawBorder();
        Console.WriteLine($"Current average reward: {averageReward}");
        Console.WriteLine($"Current distance: {distance}");
        DrawBorder();
        Console.WriteLine($"Previous average reward: {pastAverageReward}");
        Console.WriteLine($"Best distance: {bestDistance}");
        DrawBorder();
        Console.WriteLine($"Body angle: {state.GetValue(0, 0)}");
        Console.WriteLine($"Body angular velocity: {state.GetValue(1, 0)}");
        Console.WriteLine($"Body linear velocity: ({state.GetValue(0, 0)}, {state.GetValue(1, 0)})");
        Console.WriteLine($"Lower left leg angle: {state.GetValue(2, 0)}");
        Console.WriteLine($"Upper left leg angle: {state.GetValue(3, 0)}");
        Console.WriteLine($"Lower right leg angle: {state.GetValue(4, 0)}");
        Console.WriteLine($"Upper right leg angle: {state.GetValue(5, 0)}");
        DrawBorder();
        Console.WriteLine("Press 'x' to exit the training loop.");
    }

    // Adds an average reward to the list for use in data collection.
    public void AddAverageEpisodeReward(float reward)
    {
        if (Hyperparameters.CollectData) _averageRewards.Add(reward);
    }
    
    // Displays the current training information.
    private void TrainingInformation(int epoch, int batch, int batchSize, float criticLoss)
    {
        Console.Clear();

        int epochSlider = Convert.ToInt32(((float) epoch / Hyperparameters.Epochs) * 10f);
        int batchSlider = Convert.ToInt32(((float) batch / batchSize) * 10f);
        
        DrawBorder();
        Console.WriteLine($"Epoch {epoch}/{Hyperparameters.Epochs} ({new string('=', epochSlider)}{new string('-', 10 - epochSlider)})");
        Console.WriteLine($"Batch {batch}/{batchSize} ({new string('=', batchSlider)}{new string('-', 10 - batchSlider)})");
        Console.WriteLine($"Current critic loss: {criticLoss}");
        DrawBorder();
    }

    // Exit menu.
    public void ExitTraining()
    {
        Option[] options = new[]
        {
            Hyperparameters.CollectData ? new Function(SaveData, "Save data") : (Option) new Text("Cannot save data, data collection is disabled."),
            new Function(Exit, "Exit")
        };
        
        RenderMenu(options, "End of training", ExitTraining, hasText: !Hyperparameters.CollectData);
    }

    // Creates a data file from the average rewards list.
    private async void CreateDataFile(string filePath)
    {
        Hyperparameters.CreateDirectories();
        string data = string.Join(" ", _averageRewards);
        data = $"{_averageRewards.Count}\n" + data;
        await File.WriteAllTextAsync(filePath, data);
    }

    // Starting menu.
    private void StartingMenu()
    {
        Option[] options = new[]
        {
            new Function(Clear, "Start training"),
            new Function(HyperparameterMenu, "Edit hyperparameters"),
            new Function(SettingsMenu, "Edit settings"),
            new Function(LoadConfiguration, "Load configuration"),
            new Function(SaveConfiguration, "Save configuration"),
            new Function(LoadNeuralNetworks, "Load neural network weights"),
            new Function(Exit, "Exit")
        };
        
        RenderMenu(options, "Start-up", StartingMenu);
    }
    
    // Hyperparameter sub-menu.
    private void HyperparameterMenu()
    {
        Option[] options = new[]
        {
            (Option) new Function(EditNeuralNetworks, "Edit neural networks"),
            new Function(EditConstants, "Edit hyperparameter constants"),
            new Variable(Hyperparameters.UseGAE, "UseGAE", "Toggle Generalized Advantage Estimate", HyperparameterMenu),
            new Variable(Hyperparameters.NormalizeAdvantages, "NormalizeAdvantages", "Toggle normalizing advantages", HyperparameterMenu),
            new Variable(Hyperparameters.LogStandardDeviation, "LogStandardDeviation", "Edit log standard deviation", HyperparameterMenu, validationFunction: ValidateFloat),
            new Variable(Hyperparameters.BatchSize, "BatchSize", "Edit batch size", HyperparameterMenu, validationFunction: ValidateIntPositive),
            new Variable(Hyperparameters.Epochs, "Epochs", "Edit epoch count", HyperparameterMenu, validationFunction: ValidateIntPositive),
            new Function(StartingMenu, "Back"),
        };
        
        RenderMenu(options, "Hyperparameters", HyperparameterMenu);
    }

    // Neural network editing sub-menu.
    private void EditNeuralNetworks()
    {
        Option[] options = new[]
        {
            (Option) new Text("Keys:"),
            new Text("|X|: Dense layer with output X."),
            new Text("(X): Activation layer with function X."),
            new Text("(last dense layer must have output 1/4 for critic/actor respectively)"),
            new Text(""),
            new Text("Critic neural network structure:"),
            new Text(Hyperparameters.CriticNeuralNetwork),
            new Text(""),
            new Text("Actor neural network structure:"),
            new Text(Hyperparameters.ActorNeuralNetwork),
            new Text(""),
            new Variable(Hyperparameters.CriticNeuralNetwork, "CriticNeuralNetwork", "Edit critic neural network", EditNeuralNetworks, showCurrent: false, validationFunction: ValidateNeuralNetwork),
            new Variable(Hyperparameters.ActorNeuralNetwork, "ActorNeuralNetwork", "Edit actor neural network", EditNeuralNetworks, showCurrent: false, validationFunction: ValidateNeuralNetwork),
            new Function(HyperparameterMenu, "Back"),
        };
        
        RenderMenu(options, "Neural Networks", EditNeuralNetworks, hasText: true);
    }

    // Constant variables editing sub-menu.
    private void EditConstants()
    {
        Option[] options = new[]
        {
            (Option) new Text("Adam Constants:"),
            new Variable(Hyperparameters.Alpha, "Alpha", "Edit learning rate, alpha", EditConstants, validationFunction: ValidateFloatPositive),
            new Variable(Hyperparameters.Beta1, "Beta1", "Edit 1st-order exponential decay, beta1", EditConstants, validationFunction: ValidateFloat),
            new Variable(Hyperparameters.Beta2, "Beta2", "Edit 2nd-order exponential decay, beta2", EditConstants, validationFunction: ValidateFloat),
            new Variable(Hyperparameters.AdamEpsilon, "AdamEpsilon", "Edit zero-division preventative, epsilon (Adam)", EditConstants, validationFunction: ValidateFloat),
            new Text(""),
            new Text("PPO Constants:"),
            new Variable(Hyperparameters.Gamma, "Gamma", "Edit discount factor, gamma", EditConstants, validationFunction: ValidateFloat),
            new Variable(Hyperparameters.Epsilon, "Epsilon", "Edit clipping factor, epsilon", EditConstants, validationFunction: ValidateFloat),
            new Text(""),
            new Function(HyperparameterMenu, "Back"),
        };

        RenderMenu(options, "Constants", EditConstants, hasText: true);
    }

    // Settings sub-menu.
    private void SettingsMenu()
    {
        Option[] options = new[]
        {
            (Option) new Variable(Hyperparameters.GameSpeed, "GameSpeed", "Change game speed", SettingsMenu, validationFunction: ValidateIntPositive),
            new Variable(Hyperparameters.Iterations, "Iterations", "Edit physics iteration count", SettingsMenu, validationFunction: ValidateIntPositive),
            new Variable(Hyperparameters.CollectData, "CollectData", "Toggle data collection", SettingsMenu),
            new Variable(Hyperparameters.SaveWeights, "SaveWeights", "Toggle weights saving", SettingsMenu),
            new Variable(Hyperparameters.CriticWeightFileName, "CriticWeightFileName", "Change critic weights file name", SettingsMenu, validationFunction: ValidateFileName),
            new Variable(Hyperparameters.ActorWeightFileName, "ActorWeightFileName", "Change actor weights file name", SettingsMenu, validationFunction: ValidateFileName),
            new Variable(Hyperparameters.FilePath, "FilePath", "Change file path", SettingsMenu, validationFunction: ValidateFilePath),
            new Variable(Hyperparameters.RoughFloor, "RoughFloor", "Toggle rough floor", SettingsMenu),
            new Variable(Hyperparameters.MaxTimesteps, "MaxTimesteps", "Edit maximum timestep count", SettingsMenu, validationFunction: ValidateIntPositive),
            new Function(StartingMenu, "Back"),
        };
        
        RenderMenu(options, "Settings", SettingsMenu);
    }

    // Exits the program.
    public static void Exit()
    {
        Clear();
        System.Environment.Exit(0);
    }

    // Neural network weight loading sub-menu.
    private void LoadNeuralNetworks()
    {
        Option[] options = new[]
        {
            new Function(LoadCriticNetwork, "Load critic weights"),
            new Function(LoadActorNetwork, "Load actor weights"),
            new Function(StartingMenu, "Back")
        };
        
        RenderMenu(options, "Load neural networks", LoadNeuralNetworks);
    }

    // Loads critic network weights from a given file.
    private void LoadCriticNetwork()
    {
        Console.Clear();
        DrawBorder("Load critic network");
        
        Console.WriteLine("Enter the file name of the critic network to be loaded (leave blank for default, 'x' to exit):");
        DrawBorder();
        
        string fileName = Console.ReadLine();
        string fileLocation;
        
        (bool result, string error) = ValidateFileName(fileName);
        if (!result)
        {
            LoadCriticNetwork(error);
            return;
        }

        if (fileName != null && fileName.ToLower() == "x")
        {
            LoadNeuralNetworks();
            return;
        }

        if (fileName == "")
        {
            fileLocation = $"{Hyperparameters.FilePath}{WeightsLocation}{Hyperparameters.CriticWeightFileName}.weights";
        }
        else
        {
            fileLocation = $"{Hyperparameters.FilePath}{WeightsLocation}{fileName}.weights";
        }

        if (!File.Exists(fileLocation))
        {
            LoadCriticNetwork("file does not exist");
            return;
        }

        string[] criticWeights = File.ReadAllLines(fileLocation);

        if (criticWeights.Length < 2 || criticWeights == Array.Empty<string>())
        {
            LoadCriticNetwork("weights file is too small/empty");
            return;
        }

        if (criticWeights[0] != Hyperparameters.CriticNeuralNetwork)
        {
            LoadCriticNetwork("weights file does not have the same network");
            return;
        }
        
        (bool weightsResult, string weightsError) = NeuralNetwork.ValidateWeights(criticWeights);
        if (!weightsResult)
        {
            LoadCriticNetwork($"weights are invalid: {weightsError}");
            return;
        }

        Hyperparameters.CriticWeights = criticWeights;
        
        LoadNeuralNetworks();
    }
    
    // Loads critic network weights from a given file, outputting an error.
    private void LoadCriticNetwork(string error)
    {
        Console.Clear();
        DrawBorder("Load critic network");
        Console.WriteLine("Enter the file name of the critic network to be loaded (leave blank for default, 'x' to exit):");
        Console.WriteLine($"Error: {error}.");
        DrawBorder();
        
        string fileName = Console.ReadLine();
        string fileLocation;
        
        (bool result, string errorValidation) = ValidateFileName(fileName);
        if (!result)
        {
            LoadCriticNetwork(errorValidation);
            return;
        }

        if (fileName != null && fileName.ToLower() == "x")
        {
            LoadNeuralNetworks();
            return;
        }

        if (fileName == "")
        {
            fileLocation = $"{Hyperparameters.FilePath}{WeightsLocation}{Hyperparameters.CriticWeightFileName}.weights";
        }
        else
        {
            fileLocation = $"{Hyperparameters.FilePath}{WeightsLocation}{fileName}.weights";
        }

        if (!File.Exists(fileLocation))
        {
            LoadCriticNetwork("file does not exist");
            return;
        }

        string[] criticWeights = File.ReadAllLines(fileLocation);

        if (criticWeights.Length < 2 || criticWeights == Array.Empty<string>())
        {
            LoadCriticNetwork("weights file is too small/empty");
            return;
        }

        if (criticWeights[0] != Hyperparameters.CriticNeuralNetwork)
        {
            LoadCriticNetwork("weights file does not have the same network");
            return;
        }
        
        (bool weightsResult, string weightsError) = NeuralNetwork.ValidateWeights(criticWeights);
        if (!weightsResult)
        {
            LoadCriticNetwork($"weights are invalid: {weightsError}");
            return;
        }

        Hyperparameters.CriticWeights = criticWeights;
        
        LoadNeuralNetworks();
    }
    
    // Loads actor network weights from a given file.
    private void LoadActorNetwork()
    {
        Console.Clear();
        DrawBorder("Load actor network");
        
        Console.WriteLine("Enter the file name of the actor network to be loaded (leave blank for default, 'x' to exit):");
        DrawBorder();
        
        string fileName = Console.ReadLine();
        string fileLocation;

        if (fileName != null && fileName.ToLower() == "x")
        {
            LoadNeuralNetworks();
            return;
        }
        
        (bool result, string error) = ValidateFileName(fileName);
        if (!result)
        {
            LoadActorNetwork(error);
            return;
        }
        
        if (fileName == "")
        {
            fileLocation = $"{Hyperparameters.FilePath}{WeightsLocation}{Hyperparameters.ActorWeightFileName}.weights";
        }
        else
        {
            fileLocation = $"{Hyperparameters.FilePath}{WeightsLocation}{fileName}.weights";
        }

        if (!File.Exists(fileLocation))
        {
            LoadActorNetwork("file does not exist");
            return;
        }

        string[] actorWeights = File.ReadAllLines(fileLocation);

        if (actorWeights.Length < 2 || actorWeights == Array.Empty<string>())
        {
            LoadActorNetwork("weights file is too small/empty");
            return;
        }

        if (actorWeights[0] != Hyperparameters.ActorNeuralNetwork)
        {
            LoadActorNetwork("weights file does not have the same network");
            return;
        }
        
        (bool weightsResult, string weightsError) = NeuralNetwork.ValidateWeights(actorWeights);
        if (!weightsResult)
        {
            LoadActorNetwork($"weights are invalid: {weightsError}");
            return;
        }

        Hyperparameters.ActorWeights = actorWeights;
        
        LoadNeuralNetworks();
    }
    
    // Loads actor network weights from a given file, outputting an error.
    private void LoadActorNetwork(string error)
    {
        Console.Clear();
        DrawBorder("Load actor network");
        Console.WriteLine("Enter the file name of the actor network to be loaded (leave blank for default, 'x' to exit):");
        Console.WriteLine($"Error: {error}.");
        DrawBorder();
        
        string fileName = Console.ReadLine();
        string fileLocation;
        
        (bool result, string errorValidation) = ValidateFileName(fileName);
        if (!result)
        {
            LoadActorNetwork(errorValidation);
            return;
        }

        if (fileName != null && fileName.ToLower() == "x")
        {
            LoadNeuralNetworks();
            return;
        }

        if (fileName == "")
        {
            fileLocation = $"{Hyperparameters.FilePath}{WeightsLocation}{Hyperparameters.ActorWeightFileName}.weights";
        }
        else
        {
            fileLocation = $"{Hyperparameters.FilePath}{WeightsLocation}{fileName}.weights";
        }

        if (!File.Exists(fileLocation))
        {
            LoadActorNetwork("file does not exist");
            return;
        }

        string[] actorWeights = File.ReadAllLines(fileLocation);

        if (actorWeights.Length < 2 || actorWeights == Array.Empty<string>())
        {
            LoadActorNetwork("weights file is too small/empty");
            return;
        }

        if (actorWeights[0] != Hyperparameters.ActorNeuralNetwork)
        {
            LoadActorNetwork("weights file does not have the same network");
            return;
        }
        
        (bool weightsResult, string weightsError) = NeuralNetwork.ValidateWeights(actorWeights);
        if (!weightsResult)
        {
            LoadActorNetwork($"weights are invalid: {weightsError}");
            return;
        }

        Hyperparameters.ActorWeights = actorWeights;
        
        LoadNeuralNetworks();
    }

    // Saves the hyperparameters into a JSON file.
    private void SaveConfiguration()
    {
        Console.Clear();
        DrawBorder("Save configuration");
        Console.WriteLine("Enter the file name of the configuration to be saved (leave blank for default, 'x' to exit):");
        DrawBorder();

        string fileName = Console.ReadLine();
        string fileLocation;

        if (fileName != null && fileName.ToLower() == "x")
        {
            StartingMenu();
            return;
        }

        (bool result, string error) = ValidateFileName(fileName);
        if (!result)
        {
            SaveConfiguration();
            return;
        }
        
        if (fileName == "") 
        {
            fileLocation = $"{Hyperparameters.FilePath}{ConfigurationLocation}{DefaultConfigName}.json";
        }
        else
        {
            fileLocation = $"{Hyperparameters.FilePath}{ConfigurationLocation}{fileName}.json";
        }

        Hyperparameters.SerializeJson(fileLocation);
        StartingMenu();
    }
    
    // Menu for saving data.
    private void SaveData()
    {
        Console.Clear();
        DrawBorder("Save data");
        Console.WriteLine("Enter the file name for the saved data (leave blank for default, 'x' to exit):");
        DrawBorder();
        
        string fileName = Console.ReadLine();
        string fileLocation;

        if (fileName != null && fileName.ToLower() == "x")
        {
            ExitTraining();
            return;
        }
        
        (bool result, string error) = ValidateFileName(fileName);
        if (!result)
        {
            SaveData();
            return;
        }
        
        if (fileName == "")
        {
            fileLocation = $"{Hyperparameters.FilePath}{DataLocation}{DefaultDataName}.txt";
        }
        else
        {
            fileLocation = $"{Hyperparameters.FilePath}{DataLocation}{fileName}.txt";
        }
        
        CreateDataFile(fileLocation);
        ExitTraining();
    }
    
    // Loads a JSON configuration file.
    private void LoadConfiguration()
    {
        Console.Clear();
        DrawBorder("Load configuration");
        Console.WriteLine("Enter the file name of the configuration to be loaded (leave blank for default, 'x' to exit):");
        DrawBorder();
        
        string fileName = Console.ReadLine();
        string fileLocation;
        
        (bool result, string error) = ValidateFileName(fileName);
        if (!result)
        {
            LoadConfiguration();
            return;
        }

        if (fileName != null && fileName.ToLower() == "x")
        {
            StartingMenu();
            return;
        }

        if (fileName == "")
        {
            fileLocation = $"{Hyperparameters.FilePath}{ConfigurationLocation}{DefaultConfigName}.json";
        }
        else
        {
            fileLocation = $"{Hyperparameters.FilePath}{ConfigurationLocation}{fileName}.json";
        }

        if (!File.Exists(fileLocation))
        {
            LoadConfiguration();
            return;
        }
        
        Hyperparameters.DeserializeJson(fileLocation);
        StartingMenu();
    }

    // Validates an input for file names.
    public static (bool, string) ValidateFileName(string fileName, string variableName = "")
    {
        if (fileName == null || fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            return (false, "file name is invalid");
        }

        return (true, "");
    }

    // Validates an input for file paths.
    public static  (bool, string) ValidateFilePath(string filePath, string variableName = "")
    {
        if (filePath == null || !Directory.Exists(filePath) || !filePath.EndsWith("/"))
        {
            return (false, "file path is invalid");
        }

        return (true, "");
    }
    
    // Validates an input for neural networks.
    public static (bool, string) ValidateNeuralNetwork(string neuralNetwork, string variableName )
    {
        if (variableName != "CriticNeuralNetwork" && variableName != "ActorNeuralNetwork") return (false, "invalid neural network type");
        
        string pattern = @"^Input ((\|[1-9]\d*\| )|(\((LeakyReLU|TanH|ReLU)\) ))+Output$";
        bool result = Regex.IsMatch(neuralNetwork, pattern, RegexOptions.None);
        
        string error = result ? "" : "neural network not valid, check syntax";
        if (result)
        {
            Match outputDense = Regex.Match(neuralNetwork, @"\|\d+\|", RegexOptions.RightToLeft);
            if (outputDense.Length == 0) return (false, "no dense layers");
            int outputInt = Convert.ToInt32(outputDense.ToString().Trim('|'));
            int requiredOutputInt = variableName == "CriticNeuralNetwork" ? 1 : 4;

            result = outputInt == requiredOutputInt;
            error = result ? "" : $"last dense layer should output {requiredOutputInt}, currently outputs {outputInt}";
        }
        
        return (result, error);
    }

    // Validates an input for positive integers.
    public static (bool, string) ValidateIntPositive(string variable, string variableName = "")
    {
        bool isInt = int.TryParse(variable, out int variableInt);
        bool result = !(variableInt <= 0 || variableInt > 99999) && isInt;
        string error = result ? "" : $"value must be positive, an integer, and less than 100000 (inputted {variable})";
        return (result, error);
    }

    // Validates an input for positive floats.
    public static (bool, string) ValidateFloatPositive(string variable, string variableName = "")
    {
        bool isFloat = float.TryParse(variable, out float variableFloat);
        bool result = !(variableFloat <= 0f || variableFloat > 99999f) && isFloat;
        string error = result ? "" : $"value must be positive, a float, and less than 100000 (inputted {variable})";
        return (result, error);
    }

    // Validates an input for floats.
    public static (bool, string) ValidateFloat(string variable, string variableName = "")
    {
        bool isFloat = float.TryParse(variable, out float variableFloat);
        string error = isFloat ? "" : $"value must be a float (inputted {variable})";
        return (isFloat, error);
    }

    // Renders a menu from a given list of options.
    private void RenderMenu(Option[] options, string title, Action menu, bool hasText = false)
    {
        Clear();
        DrawBorder(title);
        DrawOptions(options);
        DrawBorder();

        if (hasText)
        {
            List<Option> optionsWithoutText = new List<Option>();
            for (int i = 0; i < options.Length; i++)
            {
                if (options[i].Type != OptionType.TEXT) optionsWithoutText.Add(options[i]);
            }

            options = optionsWithoutText.ToArray();
        }
        
        
        GetInput(options, menu);
    }

    // Receives the user input and activates the given option in a menu.
    private void GetInput(Option[] options, Action previousMenu)
    {
        char input = Console.ReadKey(true).KeyChar;
        input = Char.ToUpper(input);
        int result = ValidateInput(input, options.Length);

        if (result == -1) previousMenu();
        else options[result].Activate();
    }
    
    // Validates the user input for menu selection.
    private int ValidateInput(char input, int count)
    {
        // 65 == 'A'
        int inputInt = Convert.ToInt32(input);
        if (inputInt < 65 || inputInt > 64 + count) return -1;
        return inputInt - 65;
    }

    // Draws the options.
    private void DrawOptions(Option[] options)
    {
        int count = 0;
        
        foreach (Option option in options)
        {
            char letter = Convert.ToChar(65 + count);
            count++;
            
            if (option.Type == OptionType.VARIABLE)
            {
                Variable variable = (Variable) option;
                if (variable.ShowCurrent)
                {
                    Console.WriteLine($"{letter}) {option.Text} (currently: {variable.GetValue()})");
                }
                else
                {
                    Console.WriteLine($"{letter}) {option.Text}");
                }
            }
            else if (option.Type == OptionType.TEXT)
            {
                Console.WriteLine($"{option.Text}");
                count--;
            }
            else
            {
                Console.WriteLine($"{letter}) {option.Text}");
            }
        }
    }

    // Draws a line in the console.
    public static void DrawBorder()
    {
        // 30 characters long
        Console.WriteLine("------------------------------");
    }

    // Draws a line in the console with a title.
    public static void DrawBorder(string title)
    {
        if (title.Length >= 28)
        {
            Console.WriteLine(title);
            return;
        }
        
        int length = 28 - title.Length;
        length /= 2;

        string borderSide = new string('-', length);
        
        Console.WriteLine($"{borderSide} {title} {borderSide}");
    }

    // Clears the console.
    public static void Clear()
    {
        Console.Clear();
    }
}

// Option class, generic class for items inside a menu.
// We can make the menus modular so its easier to add variables/options in menus.
public abstract class Option
{
    public readonly string Text;
    protected readonly object _activation;
    public OptionType Type;

    public Option(object activation, string text, OptionType type)
    {
        Text = text;
        Type = type;
        _activation = activation;
    }

    public abstract void Activate(string errorMessage = null);
}

// Text class, an option with no activation and is used just for text.
public class Text : Option
{
    public Text(string text) : base(text, text, OptionType.TEXT) {}
    public override void Activate(string errorMessage = null) {}
}

// Function class, an option which returns a function upon activation.
public class Function : Option
{
    public Function(Action function, string text) : base(function, text, OptionType.FUNCTION) {}

    // Function which is called when the option is activated.
    // Performs a function given.
    public override void Activate(string errorMessage = null)
    {
        Action action = (Action)_activation;
        action();
    }
}

// Variable class, an option which edits a given variable upon activation.
public class Variable : Option
{
    private readonly string _variableName;
    public readonly bool ShowCurrent;
    private readonly Action _previousMenu;
    private readonly Func<string, string, (bool, string)> _validationFunction;
    
    public Variable(object value, string variableName, string text, Action previousMenu, bool showCurrent = true, Func<string, string, (bool, string)> validationFunction = null) : base(value, text, OptionType.VARIABLE)
    {
        _variableName = variableName;
        ShowCurrent = showCurrent;
        _previousMenu = previousMenu;
        _validationFunction = validationFunction;
    }

    // Function which is called when the option is activated.
    // Edits a variable given.
    public override void Activate(string errorMessage = null)
    {
        ConsoleRenderer.Clear();

        if (_activation is bool)
        {
            Hyperparameters.ReflectionSet(_variableName, ! (bool) Hyperparameters.ReflectionGet(_variableName));
            _previousMenu();
            return;
        }
        
        ConsoleRenderer.DrawBorder("Variable Edit");
        Console.WriteLine($"Currently editing: {_variableName} ('x' to exit).");
        if (errorMessage != null) Console.WriteLine($"Error: {errorMessage}.");
        ConsoleRenderer.DrawBorder();
        
        string input = Console.ReadLine();

        if (input == "x")
        {
            _previousMenu();
            return;
        }
        
        if (_validationFunction != null)
        {
            // We pass in the variable name to the validation function in case the function requires any specific information about
            // what variable is being edited. (e.g. critic/actor network validation)
            (bool result, string error) = _validationFunction(input, _variableName);
            if (!result)
            {
                Activate(error);
                return;
            }
        }
        
        var value = Convert.ChangeType(input, _activation.GetType());

        Hyperparameters.ReflectionSet(_variableName, value);
        _previousMenu();
    }

    // Returns the current value of the variable.
    public object GetValue()
    {
        return _activation;
    }
}

// Option Type enum, used for finding out which child class a generic 'Option' class is.
public enum OptionType
{
    VARIABLE,
    FUNCTION,
    TEXT
}