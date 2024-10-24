using System;
using System.IO;
using System.Text.Json;
using NEA.Rendering;

namespace NEA.Walker.PPO;

// A non-static version of the hyperparameters class for use in serializing (converting to JSON).
// We need to have a class instance to serialize it, and so this is used when saving/loading JSON files.
[Serializable]
public class SerializableHyperparameters
{
    // Settings
    public int GameSpeed { get; set; }
    public bool CollectData { get; set; }
    public bool SaveWeights { get; set; }

    public int Iterations { get; set; }
    public int MaxTimesteps { get; set; }
    public bool RoughFloor { get; set; }

    // Input: 12 inputs, Output: 4 outputs, |X|: a dense layer with an output of X and input of previous in line, (X): an activation function X layer
    public string CriticNeuralNetwork { get; set; }
    public string ActorNeuralNetwork { get; set; }
    
    public string CriticWeightFileName { get; set; }
    public string ActorWeightFileName { get; set; }
    public string FilePath { get; set; }

    // Adam hyperparameters
    // Adam paper states "Good default settings for the tested machine learning problems are alpha=0.001, beta1=0.9, beta2=0.999 and epsilon=1e−8f"
    public float Alpha { get; set; } // learning rate
    public float Beta1 { get; set; } // 1st-order exponential decay
    public float Beta2 { get; set; } // 2nd-order exponential decay
    public float AdamEpsilon { get; set; } // prevent zero division
    
    // Agent hyperparameters
    public int Epochs { get; set; }
    public int BatchSize { get; set; }
    public bool UseGAE { get; set; }
    public bool NormalizeAdvantages { get; set; }
    
    // GAE hyperparameters
    public float Gamma { get; set; } // Discount Factor
    public float Lambda { get; set; } // Smoothing Factor
    
    // PPO hyperparameters
    public float Epsilon { get; set; } // Clipping Factor
    public float LogStandardDeviation { get; set; }

    public SerializableHyperparameters()
    {
        GameSpeed = Hyperparameters.GameSpeed;
        CollectData = Hyperparameters.CollectData;
        SaveWeights = Hyperparameters.SaveWeights;
        Iterations = Hyperparameters.Iterations;
        RoughFloor = Hyperparameters.RoughFloor;
        MaxTimesteps = Hyperparameters.MaxTimesteps;
        CriticNeuralNetwork = Hyperparameters.CriticNeuralNetwork;
        ActorNeuralNetwork = Hyperparameters.ActorNeuralNetwork;
        Alpha = Hyperparameters.Alpha;
        Beta1 = Hyperparameters.Beta1;
        Beta2 = Hyperparameters.Beta2;
        AdamEpsilon = Hyperparameters.AdamEpsilon;
        Epochs = Hyperparameters.Epochs;
        BatchSize = Hyperparameters.BatchSize;
        UseGAE = Hyperparameters.UseGAE;
        NormalizeAdvantages = Hyperparameters.NormalizeAdvantages;
        Gamma = Hyperparameters.Gamma;
        Lambda = Hyperparameters.Lambda;
        Epsilon = Hyperparameters.Epsilon;
        LogStandardDeviation = Hyperparameters.LogStandardDeviation;
        CriticWeightFileName = Hyperparameters.CriticWeightFileName;
        ActorWeightFileName = Hyperparameters.ActorWeightFileName;
        FilePath = Hyperparameters.FilePath;
    }
}

// Hyperparameters class, holds all settings and values which can be changed by the user.
public class Hyperparameters
{
    // Settings
    public static int GameSpeed = 1;
    public static bool CollectData = true;
    public static bool SaveWeights = true;
    public static int Iterations = 50;
    public static int MaxTimesteps = 1000;
    public static bool RoughFloor = false;
    
    // Input: 12 inputs, Output: 4 outputs, |X|: a dense layer with an output of X and input of previous in line, (X): an activation function X layer
    public static string CriticNeuralNetwork = "Input |64| (LeakyReLU) |1| Output";
    public static string ActorNeuralNetwork = "Input |64| (LeakyReLU) |64| (LeakyReLU) |4| (TanH) Output";

    public static string CriticWeightFileName = "critic";
    public static string ActorWeightFileName = "actor";
    public static string FilePath = AppDomain.CurrentDomain.BaseDirectory;

    // Not stored, used specifically for loading weights.
    public static string[] CriticWeights = Array.Empty<string>();
    public static string[] ActorWeights = Array.Empty<string>();

    // Adam hyperparameters
    // Adam paper states "Good default settings for the tested machine learning problems are alpha=0.001, beta1=0.9, beta2=0.999 and epsilon=1e−8f"
    public static float Alpha = 0.001f; // learning rate
    public static float Beta1 = 0.9f; // 1st-order exponential decay
    public static float Beta2 = 0.999f; // 2nd-order exponential decay
    public static float AdamEpsilon = 1e-8f; // prevent zero division
    
    // Agent hyperparameters
    public static int Epochs = 5;
    public static int BatchSize = 64;
    public static bool UseGAE = false;
    public static bool NormalizeAdvantages = false;
    
    // GAE hyperparameters
    public static float Gamma = 0.9f; // Discount Factor
    public static float Lambda = 0.95f; // Smoothing Factor
    
    // PPO hyperparameters
    public static float Epsilon = 0.3f; // Clipping Factor
    public static float LogStandardDeviation = -1f;

    // Converts the hyperparameters into a JSON file.
    public static async void SerializeJson(string fileLocation)
    {
        CreateDirectories();
        SerializableHyperparameters hyperparameters = new SerializableHyperparameters();
        using FileStream createStream = File.Create(fileLocation);
        await JsonSerializer.SerializeAsync(createStream, hyperparameters, new JsonSerializerOptions { WriteIndented = true });
        await createStream.DisposeAsync();
    }
    
    // Loads the hyperparameters from a JSON file.
    // Cannot be async as it needs to interrupt the program in case of an exception.
    public static void DeserializeJson(string fileLocation)
    {
        using FileStream openStream = File.OpenRead(fileLocation);
        SerializableHyperparameters hyperparameters;
        try
        {
            hyperparameters = JsonSerializer.Deserialize<SerializableHyperparameters>(openStream);
        }
        catch (Exception exception)
        {
            ErrorLogger.LogError($"JSON deserializer error. ({exception.Message})");
            return;
        }

        try
        {
            if (hyperparameters != null)
            {
                ValidateHyperparameterValues(hyperparameters);
                
                GameSpeed = hyperparameters.GameSpeed;
                CollectData = hyperparameters.CollectData;
                SaveWeights = hyperparameters.SaveWeights;
                Iterations = hyperparameters.Iterations;
                RoughFloor = hyperparameters.RoughFloor;
                MaxTimesteps = hyperparameters.MaxTimesteps;
                CriticNeuralNetwork = hyperparameters.CriticNeuralNetwork;
                ActorNeuralNetwork = hyperparameters.ActorNeuralNetwork;
                FilePath = hyperparameters.FilePath;
                Alpha = hyperparameters.Alpha;
                Beta1 = hyperparameters.Beta1;
                Beta2 = hyperparameters.Beta2;
                AdamEpsilon = hyperparameters.AdamEpsilon;
                Epochs = hyperparameters.Epochs;
                BatchSize = hyperparameters.BatchSize <= 0 ? 64 : hyperparameters.BatchSize;
                UseGAE = hyperparameters.UseGAE;
                NormalizeAdvantages = hyperparameters.NormalizeAdvantages;
                Gamma = hyperparameters.Gamma;
                Lambda = hyperparameters.Lambda;
                Epsilon = hyperparameters.Epsilon;
                LogStandardDeviation = hyperparameters.LogStandardDeviation;
                CriticWeightFileName = hyperparameters.CriticWeightFileName;
                ActorWeightFileName = hyperparameters.ActorWeightFileName;
            }
        }
        catch (Exception e)
        {
            ErrorLogger.LogError($"Exception occurred while setting the values of the hyperparameters during deserialization: ({e.Message})");
            return;
        }
        
        ValidateVariables();
    }

    private static void ValidateHyperparameterValues(SerializableHyperparameters hyperparameters)
    {
        if (hyperparameters.GameSpeed <= 0 || hyperparameters.GameSpeed >= 10) 
            throw new Exception($"Invalid game speed, should be in range 0<x<10 ({hyperparameters.GameSpeed})");
        if (hyperparameters.Iterations <= 0 || hyperparameters.Iterations >= 200)
            throw new Exception($"Invalid iterations count, should be in range 0<x<200 ({hyperparameters.Iterations})");
        if (hyperparameters.MaxTimesteps <= 0) 
            throw new Exception($"Invalid maximum time steps amount, should be in range x>0 ({hyperparameters.MaxTimesteps})");
        if (hyperparameters.Alpha <= 0 || hyperparameters.Alpha >= 10)
            throw new Exception($"Invalid alpha value, should be in range 0<x<10 ({hyperparameters.Alpha})");
        if (hyperparameters.Beta1 <= 0 || hyperparameters.Beta1 > 1)
            throw new Exception($"Invalid beta1 value, should be in range 0<x<1 ({hyperparameters.Beta1}");
        if (hyperparameters.Beta2 <= 0 || hyperparameters.Beta2 > 1)
            throw new Exception($"Invalid beta2 value, should be in range 0<x<1 ({hyperparameters.Beta2})");
        if (hyperparameters.AdamEpsilon <= 0 || hyperparameters.AdamEpsilon >= 1)
            throw new Exception($"Invalid Adam epsilon value, should be in range 0<x<1 ({hyperparameters.AdamEpsilon})");
        if (hyperparameters.Epochs <= 0 || hyperparameters.Epochs >= 50)
            throw new Exception($"Invalid epochs value, should be in range 0<x<50 ({hyperparameters.Epochs})");
        if (hyperparameters.BatchSize <= 0 || hyperparameters.BatchSize >= 1000)
            throw new Exception($"Invalid batch size value, should be in range 0<x<1000 ({hyperparameters.BatchSize})");
        if (hyperparameters.Gamma <= 0 || hyperparameters.Gamma > 1)
            throw new Exception($"Invalid gamma value, should be in range 0<x<1 ({hyperparameters.Gamma}");
        if (hyperparameters.Lambda <= 0 || hyperparameters.Lambda > 1)
            throw new Exception($"Invalid lambda value, should be in range 0<x<1 ({hyperparameters.Lambda}");
        if (hyperparameters.Epsilon <= 0 || hyperparameters.Epsilon > 1)
            throw new Exception($"Invalid epslion value, should be in range 0<x<1 ({hyperparameters.Epsilon}");
        if (hyperparameters.LogStandardDeviation <= -5 || hyperparameters.LogStandardDeviation >= 5)
            throw new Exception($"Invalid log standard deviation value, should be in range -5<x<5 ({LogStandardDeviation}");
    }
    
    // Creates the directories required for saving files.
    public static void CreateDirectories()
    {
        if (!Directory.Exists(Hyperparameters.FilePath + "Data/Configurations/"))
        {
            Directory.CreateDirectory(Hyperparameters.FilePath + "Data/Configurations/");
        }
        if (!Directory.Exists(Hyperparameters.FilePath + "Data/SavedData/"))
        {
            Directory.CreateDirectory(Hyperparameters.FilePath + "Data/SavedData/");
        }
        if (!Directory.Exists(Hyperparameters.FilePath + "Data/Weights/"))
        {
            Directory.CreateDirectory(Hyperparameters.FilePath + "Data/Weights/");
        }
        if (!Directory.Exists(Hyperparameters.FilePath + "Logs/"))
        {
            Directory.CreateDirectory(Hyperparameters.FilePath + "Logs/");
        }
    }

    // Validates variables which would not cause JSON exceptions after deserializing but will cause issues
    // when the program starts.
    private static void ValidateVariables()
    {
        (bool resultFilePath, string errorFilePath) = ConsoleRenderer.ValidateFilePath(FilePath);
        if (!resultFilePath)
        {
            ErrorLogger.LogError($"Invalid file path for the program. ({errorFilePath})");
            FilePath = AppDomain.CurrentDomain.BaseDirectory;
        }

        (bool resultCriticName, string errorCriticName) = ConsoleRenderer.ValidateFileName(CriticWeightFileName);
        (bool resultActorName, string errorActorName) = ConsoleRenderer.ValidateFileName(ActorWeightFileName);
        if (!resultActorName || !resultCriticName)
        {
            string colon = !resultActorName && !resultCriticName ? "; " : "";
            if (!resultActorName)
            {
                errorActorName = "actor weights " + errorActorName;
                ActorWeightFileName = "actor";
            }

            if (!resultCriticName)
            {
                errorCriticName = "critic weights " + errorCriticName;
                CriticWeightFileName = "critic";
            }
            
            ErrorLogger.LogError($"Invalid file names. ({errorActorName}{colon}{errorCriticName})");
        }
        
        (bool resultCritic, string errorCritic) = ConsoleRenderer.ValidateNeuralNetwork(CriticNeuralNetwork, "CriticNeuralNetwork");
        (bool resultActor, string errorActor) = ConsoleRenderer.ValidateNeuralNetwork(ActorNeuralNetwork, "ActorNeuralNetwork");
        if (!resultActor || !resultCritic)
        {
            string colon = !resultActor && !resultCritic ? "; " : "";
            if (!resultActor)
            {
                errorActor = "actor " + errorActor;
                ActorNeuralNetwork = "Input |64| (LeakyReLU) |64| (LeakyReLU) |4| (TanH) Output";
            }

            if (!resultCritic)
            {
                errorCritic = "critic " + errorCritic;
                CriticNeuralNetwork = "Input |64| (LeakyReLU) |1| Output";
            }
            
            ErrorLogger.LogError($"Invalid neural networks. {errorActor}{colon}{errorCritic}");
        }
    }

    // Sets a variable from its given name in a string.
    public static void Set(string variableName, object value)
    {
        switch (variableName)
        {
            case "Alpha":
                Alpha = (float) value;
                break;
            case "Beta1":
                Beta1 = (float) value;
                break;
            case "Beta2":
                Beta2 = (float) value;
                break; 
            case "AdamEpsilon":
                AdamEpsilon = (float) value;
                break;
            case "Epochs":
                Epochs = (int) value;
                break;
            case "BatchSize":
                BatchSize = (int) value;
                break;
            case "UseGAE":
                UseGAE = (bool) value;
                break;
            case "NormalizeAdvantages":
                NormalizeAdvantages = (bool) value;
                break;
            case "Gamma":
                Gamma = (float) value;
                break;
            case "Lambda":
                Lambda = (float) value;
                break;
            case "Epsilon":
                Epsilon = (float) value;
                break;
            case "LogStandardDeviation":
                LogStandardDeviation = (float) value;
                break;
            case "GameSpeed":
                GameSpeed = (int) value;
                break;
            case "CollectData":
                CollectData = (bool) value;
                break;
            case "SaveWeights":
                SaveWeights = (bool) value;
                break;
            case "RoughFloor":
                RoughFloor = (bool) value;
                break;
            case "Iterations":
                Iterations = (int) value;
                break;
            case "MaxTimesteps":
                MaxTimesteps = (int) value;
                break;
            case "CriticNeuralNetwork":
                CriticNeuralNetwork = (string) value;
                break;
            case "ActorNeuralNetwork":
                ActorNeuralNetwork = (string) value;
                break;
            case "CriticWeightFileName":
                CriticWeightFileName = (string)value;
                break;
            case "ActorWeightFileName":
                ActorWeightFileName = (string) value;
                break;
            case "FilePath":
                FilePath = (string) value;
                break;
            // Variable name is invalid. Throw the error up the stack to be caught in a place which would
            // not interrupt the process of the program.
            default:
                ErrorLogger.LogError("Inputted an invalid variable for a hyperparameter set call.");
                throw new Exception("Inputted an invalid variable for a hyperparameter set call.");
        }
    }
    
    // Gets the value from a variable's given string.
    public static object Get(string variableName)
    {
        switch (variableName)
        {
            case "Alpha":
                return Alpha;
            case "Beta1":
                return Beta1;
            case "Beta2":
                return Beta2;
            case "AdamEpsilon":
                return AdamEpsilon;
            case "Epochs":
                return Epochs;
            case "BatchSize":
                return BatchSize;
            case "UseGAE":
                return UseGAE;
            case "NormalizeAdvantages":
                return NormalizeAdvantages;
            case "Gamma":
                return Gamma;
            case "Lambda":
                return Lambda;
            case "Epsilon":
                return Epsilon;
            case "LogStandardDeviation":
                return LogStandardDeviation;
            case "FastForward":
                return GameSpeed;
            case "CollectData":
                return CollectData;
            case "SaveWeights":
                return SaveWeights;
            case "RoughFloor":
                return RoughFloor;
            case "Iterations":
                return Iterations;
            case "MaxTimesteps":
                return MaxTimesteps;
            case "ActorNeuralNetwork":
                return ActorNeuralNetwork;
            case "CriticNeuralNetwork":
                return CriticNeuralNetwork;
            case "ActorWeightFileName":
                return ActorWeightFileName;
            case "CriticWeightFileName":
                return CriticWeightFileName;
            case "FilePath":
                return FilePath;
            // Variable name is invalid. Throw the error up the stack to be caught in a place which would
            // not interrupt the process of the program.
            default:
                ErrorLogger.LogError($"Inputted an invalid variable for a hyperparameter get call ({variableName}).");
                throw new Exception($"Inputted an invalid variable for a hyperparameter get call ({variableName}).");
        }
    }
}