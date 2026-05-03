using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using LLama;
using LLama.Common;
using LLama.Sampling;

//Main file for handling communication with the trained DM model.
//Looks in appropriate folder for the .gguf file, loads it and calls using LLamaSharp when its DM turn
public class DMBrain : MonoBehaviour
{
    [Header("Model Settings")]
    [Tooltip("Filename of your .gguf file inside StreamingAssets/Models/")]
    public string modelFileName = "dm_boss_gguf-unsloth.Q4_K_M.gguf";

    [Tooltip("Max new tokens the model generates per response.")]
    public int maxTokens = 150;

    [Tooltip("Low = consistent JSON. 0.15 recommended for fine-tuned classification.")]
    [Range(0.05f, 1.0f)]
    public float temperature = 0.15f;

    [Tooltip("Context window size. Must match max_seq_length used during training (768).")]
    public uint contextSize = 768;

    [Tooltip("Must match the dm_name used during training (default: 'The Dungeon Master').")]
    public string dmName = "The Dungeon Master";

    public bool IsReady { get; private set; } = false;

    private LLamaWeights weights;
    private ModelParams  modelParams;
    private bool         isBusy = false;

    // Matches the system prompt in dnd_training.py exactly
    private const string SYSTEM_PROMPT =
        "You are the Dungeon Master, a powerful boss in a D&D 5e encounter. " +
        "Each turn you observe the player's actions — their ability, weapon, and any spells. " +
        "Your goal is to identify their class as quickly and accurately as possible. " +
        "Once you are confident, declare your hypothesis. " +
        "Respond with valid JSON only containing: class_hypothesis, confidence, and reasoning.";

    // ─────────────────────────────────────────────────────────────

    //Main bootup/setup method - loads model and prepares it
    private async void Start()
    {
        await LoadModelAsync();
    }

    private void OnDestroy()
    {
        weights?.Dispose();
    }

    private async Task LoadModelAsync()
    {
        string modelPath = Path.Combine(Application.streamingAssetsPath, "Models", modelFileName);

        if (!File.Exists(modelPath))
        {
            Debug.LogError($"[DMBrain] Model not found at: {modelPath}");
            return;
        }

        Debug.Log($"[DMBrain] Loading model...");

        try
        {
            await Task.Run(() =>
            {
                modelParams = new ModelParams(modelPath)
                {
                    ContextSize   = contextSize,
                    GpuLayerCount = 0
                };
                weights = LLamaWeights.LoadFromFile(modelParams);
            });

            IsReady = true;
            Debug.Log($"[DMBrain] Model ready — took {Time.realtimeSinceStartup:F1}s since scene load.");
        }
        catch (Exception e)
        {
            Debug.LogError($"[DMBrain] Load failed: {e.Message}");
        }
    }

    //Public API — called by AIManager

    //Analyses the actions/observed things the DM has seen during the turn
    public async Task<DMHypothesis> AnalyseAsync(string observationJson)
    {
        if (!IsReady)  { Debug.LogWarning("[DMBrain] Not ready."); return null; }
        if (isBusy)   { Debug.LogWarning("[DMBrain] Busy — skipping turn."); return null; }

        isBusy = true;
        try
        {
            //observationJson already contains the "TURN X OBSERVATION" header
            string raw = await RunInferenceAsync(observationJson);
            Debug.Log($"[DMBrain] Raw output: {raw}");
            return ParseHypothesis(raw);
        }
        catch (Exception e)
        {
            Debug.LogError($"[DMBrain] Inference error: {e.Message}");
            return null;
        }
        finally
        {
            isBusy = false;
        }
    }

    //Inference - way of getting the models response back based on the prompt we sent
    private async Task<string> RunInferenceAsync(string userContent)
    {
        string prompt =
            $"<|im_start|>system\n{SYSTEM_PROMPT}<|im_end|>\n" +
            $"<|im_start|>user\n{userContent}<|im_end|>\n" +
            "<|im_start|>assistant\n";

        var result = new StringBuilder();

        await Task.Run(() =>
        {
            var executor = new StatelessExecutor(weights, modelParams);

            var inferenceParams = new InferenceParams
            {
                MaxTokens        = maxTokens,
                SamplingPipeline = new DefaultSamplingPipeline { Temperature = temperature },
                //Stop at ChatML turn boundaries
                AntiPrompts = new[] { "<|im_end|>", "<|im_start|>" }
            };

            var enumerator = executor.InferAsync(prompt, inferenceParams).GetAsyncEnumerator();
            int tokenCount = 0;

            try
            {
                while (enumerator.MoveNextAsync().AsTask().GetAwaiter().GetResult())
                {
                    string token = enumerator.Current;
                    result.Append(token);
                    tokenCount++;

                    string current = result.ToString();

                    //Early exit: we have a complete JSON object
                    int openBrace  = current.IndexOf('{');
                    int closeBrace = current.LastIndexOf('}');
                    if (openBrace >= 0 && closeBrace > openBrace)
                        break;

                    //Safety exit: if we've generated a lot of tokens with no opening brace yet, something has gone wrong — bail out
                    if (tokenCount > 30 && openBrace < 0)
                    {
                        Debug.LogWarning("[DMBrain] No JSON started after 30 tokens — aborting.");
                        break;
                    }
                }
            }
            finally
            {
                enumerator.DisposeAsync().AsTask().GetAwaiter().GetResult();
            }
        });

        return result.ToString().Trim();
    }

    //JSON parsing - getting models response and turning it back to readable text

    private DMHypothesis ParseHypothesis(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return null;

        int start = raw.IndexOf('{');
        int end   = raw.LastIndexOf('}');
        if (start < 0 || end <= start) { Debug.LogWarning($"[DMBrain] No JSON in: {raw}"); return null; }

        string json = raw.Substring(start, end - start + 1);
        try
        {
            DMHypothesis h = JsonUtility.FromJson<DMHypothesis>(json);
            if (h == null || string.IsNullOrEmpty(h.class_hypothesis))
            {
                Debug.LogWarning($"[DMBrain] Empty hypothesis from: {json}");
                return null;
            }
            return h;
        }
        catch (Exception e)
        {
            Debug.LogError($"[DMBrain] Parse failed: {e.Message} — {json}");
            return null;
        }
    }
}

[Serializable]
public class DMHypothesis
{
    public string class_hypothesis;
    public float  confidence;
    public string reasoning;

    public string ClassHypothesis => class_hypothesis;
    public float  Confidence      => confidence;
    public string Reasoning       => reasoning;
}