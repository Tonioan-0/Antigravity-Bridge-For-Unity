using System;

namespace AntigravityBridge.Editor.Models
{
    /// <summary>
    /// Editor state information for AI awareness
    /// </summary>
    [Serializable]
    public class EditorStateData
    {
        public bool is_playing;
        public bool is_paused;
        public bool is_compiling;
        public string last_compilation_result;  // "success", "error", "none"
        public bool has_errors;
        public bool has_warnings;
    }

    /// <summary>
    /// Console log entry from Unity Editor
    /// </summary>
    [Serializable]
    public class ConsoleLogEntry
    {
        public string type;        // "error", "warning", "info", "exception"
        public string message;
        public string stackTrace;
        public string timestamp;
        public int count;          // How many times this message was logged (collapsed)
    }

    /// <summary>
    /// Console logs response data
    /// </summary>
    [Serializable]
    public class ConsoleLogsData
    {
        public ConsoleLogEntry[] logs;
        public ConsoleSummary summary;
    }

    /// <summary>
    /// Summary of console log counts
    /// </summary>
    [Serializable]
    public class ConsoleSummary
    {
        public int errors;
        public int warnings;
        public int info;
        public int exceptions;
    }

    /// <summary>
    /// Compilation status data
    /// </summary>
    [Serializable]
    public class CompilationData
    {
        public bool is_compiling;
        public float progress;           // 0.0 to 1.0
        public string status;            // "idle", "compiling", "done"
        public bool has_errors;
        public string[] compilation_errors;
    }

    /// <summary>
    /// Wait compilation result
    /// </summary>
    [Serializable]
    public class WaitCompilationResult
    {
        public bool completed;           // true if compilation finished, false if timeout
        public float wait_time_seconds;
        public bool has_errors;
        public string[] errors;
    }

    /// <summary>
    /// Query options for filtering and formatting responses
    /// </summary>
    [Serializable]
    public class QueryOptions
    {
        public string[] select;          // Fields to include (e.g., "name", "path", "components")
        public int depth = -1;           // -1 = all, 0 = target only, 1+ = levels of children
        public string format = "full";   // "full", "minimal", "names_only", "exists_only"
        public int limit = 100;          // Max results
        public int offset = 0;           // Pagination offset
        public int precision = 3;        // Decimal places for position/rotation/scale
    }
}
