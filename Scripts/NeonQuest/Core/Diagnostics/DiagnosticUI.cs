using UnityEngine;
using UnityEngine.UI;
using TMPro;
using NeonQuest.Core.ErrorHandling;

namespace NeonQuest.Core.Diagnostics
{
    /// <summary>
    /// Real-time diagnostic UI for monitoring system performance and status
    /// </summary>
    public class DiagnosticUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Canvas _diagnosticCanvas;
        [SerializeField] private TextMeshProUGUI _performanceText;
        [SerializeField] private TextMeshProUGUI _systemStatusText;
        [SerializeField] private TextMeshProUGUI _errorLogText;
        [SerializeField] private Slider _frameRateSlider;
        [SerializeField] private Slider _memorySlider;
        [SerializeField] private Button _toggleButton;
        [SerializeField] private Button _clearLogsButton;
        [SerializeField] private ScrollRect _logScrollRect;

        [Header("Settings")]
        [SerializeField] private bool _showOnStart = false;
        [SerializeField] private float _updateInterval = 0.5f;
        [SerializeField] private int _maxLogEntries = 50;
        [SerializeField] private KeyCode _toggleKey = KeyCode.F12;

        private PerformanceMonitor _performanceMonitor;
        private float _lastUpdateTime;
        private System.Collections.Generic.Queue<string> _logEntries = new System.Collections.Generic.Queue<string>();
        private bool _isVisible;

        private void Awake()
        {
            _performanceMonitor = FindObjectOfType<PerformanceMonitor>();
            if (_performanceMonitor == null)
            {
                NeonQuestLogger.LogWarning("No PerformanceMonitor found, creating one", 
                    NeonQuestLogger.LogCategory.General, this);
                _performanceMonitor = gameObject.AddComponent<PerformanceMonitor>();
            }

            SetupUI();
            SetVisible(_showOnStart);
        }

        private void Start()
        {
            if (_performanceMonitor != null)
            {
                _performanceMonitor.OnMetricsUpdated += UpdatePerformanceDisplay;
                _performanceMonitor.OnPerformanceWarning += HandlePerformanceWarning;
                _performanceMonitor.OnPerformanceCritical += HandlePerformanceCritical;
            }

            // Subscribe to Unity's log messages
            Application.logMessageReceived += HandleLogMessage;
        }

        private void Update()
        {
            if (Input.GetKeyDown(_toggleKey))
            {
                ToggleVisibility();
            }

            if (_isVisible && Time.time - _lastUpdateTime >= _updateInterval)
            {
                UpdateSystemStatus();
                _lastUpdateTime = Time.time;
            }
        }

        private void SetupUI()
        {
            if (_diagnosticCanvas == null)
            {
                CreateDiagnosticCanvas();
            }

            if (_toggleButton != null)
            {
                _toggleButton.onClick.AddListener(ToggleVisibility);
            }

            if (_clearLogsButton != null)
            {
                _clearLogsButton.onClick.AddListener(ClearLogs);
            }

            // Set up sliders
            if (_frameRateSlider != null)
            {
                _frameRateSlider.minValue = 0f;
                _frameRateSlider.maxValue = 120f;
            }

            if (_memorySlider != null)
            {
                _memorySlider.minValue = 0f;
                _memorySlider.maxValue = 1024f; // 1GB in MB
            }
        }

        private void CreateDiagnosticCanvas()
        {
            GameObject canvasGO = new GameObject("DiagnosticCanvas");
            _diagnosticCanvas = canvasGO.AddComponent<Canvas>();
            _diagnosticCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _diagnosticCanvas.sortingOrder = 1000; // Ensure it's on top

            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();

            // Create basic UI structure
            CreateBasicUIElements();
        }

        private void CreateBasicUIElements()
        {
            // Create main panel
            GameObject panelGO = new GameObject("DiagnosticPanel");
            panelGO.transform.SetParent(_diagnosticCanvas.transform, false);
            
            Image panelImage = panelGO.AddComponent<Image>();
            panelImage.color = new Color(0, 0, 0, 0.8f);

            RectTransform panelRect = panelGO.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0, 0.5f);
            panelRect.anchorMax = new Vector2(0.4f, 1f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            // Create performance text
            GameObject perfTextGO = new GameObject("PerformanceText");
            perfTextGO.transform.SetParent(panelGO.transform, false);
            _performanceText = perfTextGO.AddComponent<TextMeshProUGUI>();
            _performanceText.text = "Performance: Initializing...";
            _performanceText.fontSize = 14;
            _performanceText.color = Color.white;

            RectTransform perfTextRect = perfTextGO.GetComponent<RectTransform>();
            perfTextRect.anchorMin = new Vector2(0, 0.8f);
            perfTextRect.anchorMax = new Vector2(1, 1f);
            perfTextRect.offsetMin = new Vector2(10, 0);
            perfTextRect.offsetMax = new Vector2(-10, -10);

            // Create system status text
            GameObject statusTextGO = new GameObject("SystemStatusText");
            statusTextGO.transform.SetParent(panelGO.transform, false);
            _systemStatusText = statusTextGO.AddComponent<TextMeshProUGUI>();
            _systemStatusText.text = "System: OK";
            _systemStatusText.fontSize = 12;
            _systemStatusText.color = Color.green;

            RectTransform statusTextRect = statusTextGO.GetComponent<RectTransform>();
            statusTextRect.anchorMin = new Vector2(0, 0.6f);
            statusTextRect.anchorMax = new Vector2(1, 0.8f);
            statusTextRect.offsetMin = new Vector2(10, 0);
            statusTextRect.offsetMax = new Vector2(-10, 0);

            // Create error log text
            GameObject logTextGO = new GameObject("ErrorLogText");
            logTextGO.transform.SetParent(panelGO.transform, false);
            _errorLogText = logTextGO.AddComponent<TextMeshProUGUI>();
            _errorLogText.text = "Logs:\n";
            _errorLogText.fontSize = 10;
            _errorLogText.color = Color.white;

            RectTransform logTextRect = logTextGO.GetComponent<RectTransform>();
            logTextRect.anchorMin = new Vector2(0, 0);
            logTextRect.anchorMax = new Vector2(1, 0.6f);
            logTextRect.offsetMin = new Vector2(10, 10);
            logTextRect.offsetMax = new Vector2(-10, 0);
        }

        private void UpdatePerformanceDisplay(PerformanceMonitor.PerformanceMetrics metrics)
        {
            if (_performanceText != null)
            {
                _performanceText.text = $"FPS: {metrics.FrameRate:F1}\n" +
                                       $"Frame Time: {metrics.FrameTime:F2}ms\n" +
                                       $"Memory: {metrics.MemoryUsage / 1024 / 1024}MB\n" +
                                       $"GameObjects: {metrics.ActiveGameObjects}";

                // Color code based on performance
                if (metrics.FrameRate < 30f)
                    _performanceText.color = Color.red;
                else if (metrics.FrameRate < 45f)
                    _performanceText.color = Color.yellow;
                else
                    _performanceText.color = Color.green;
            }

            // Update sliders
            if (_frameRateSlider != null)
            {
                _frameRateSlider.value = metrics.FrameRate;
            }

            if (_memorySlider != null)
            {
                _memorySlider.value = metrics.MemoryUsage / 1024 / 1024; // Convert to MB
            }
        }

        private void UpdateSystemStatus()
        {
            if (_systemStatusText == null) return;

            string status = "System: ";
            Color statusColor = Color.green;

            // Check various system conditions
            if (_performanceMonitor != null && _performanceMonitor.CurrentMetrics != null)
            {
                var metrics = _performanceMonitor.CurrentMetrics;
                
                if (metrics.FrameRate < 30f)
                {
                    status += "CRITICAL PERFORMANCE";
                    statusColor = Color.red;
                }
                else if (metrics.FrameRate < 45f)
                {
                    status += "PERFORMANCE WARNING";
                    statusColor = Color.yellow;
                }
                else if (metrics.MemoryUsage > 512 * 1024 * 1024) // 512MB
                {
                    status += "HIGH MEMORY USAGE";
                    statusColor = Color.yellow;
                }
                else
                {
                    status += "OK";
                    statusColor = Color.green;
                }
            }
            else
            {
                status += "MONITORING DISABLED";
                statusColor = Color.gray;
            }

            _systemStatusText.text = status;
            _systemStatusText.color = statusColor;
        }

        private void HandlePerformanceWarning(PerformanceMonitor.PerformanceMetrics metrics)
        {
            AddLogEntry($"[WARNING] Performance: {metrics.FrameRate:F1} FPS", Color.yellow);
        }

        private void HandlePerformanceCritical(PerformanceMonitor.PerformanceMetrics metrics)
        {
            AddLogEntry($"[CRITICAL] Performance: {metrics.FrameRate:F1} FPS", Color.red);
        }

        private void HandleLogMessage(string logString, string stackTrace, LogType type)
        {
            // Only show errors and warnings in the diagnostic UI
            if (type == LogType.Error || type == LogType.Warning || type == LogType.Exception)
            {
                Color logColor = type == LogType.Error || type == LogType.Exception ? Color.red : Color.yellow;
                AddLogEntry($"[{type.ToString().ToUpper()}] {logString}", logColor);
            }
        }

        private void AddLogEntry(string message, Color color)
        {
            string timestamp = System.DateTime.Now.ToString("HH:mm:ss");
            string colorHex = ColorUtility.ToHtmlStringRGB(color);
            string formattedEntry = $"<color=#{colorHex}>{timestamp}: {message}</color>";

            _logEntries.Enqueue(formattedEntry);

            while (_logEntries.Count > _maxLogEntries)
            {
                _logEntries.Dequeue();
            }

            UpdateLogDisplay();
        }

        private void UpdateLogDisplay()
        {
            if (_errorLogText != null)
            {
                _errorLogText.text = "Logs:\n" + string.Join("\n", _logEntries.ToArray());
                
                // Auto-scroll to bottom
                if (_logScrollRect != null)
                {
                    Canvas.ForceUpdateCanvases();
                    _logScrollRect.verticalNormalizedPosition = 0f;
                }
            }
        }

        private void ToggleVisibility()
        {
            SetVisible(!_isVisible);
        }

        private void SetVisible(bool visible)
        {
            _isVisible = visible;
            if (_diagnosticCanvas != null)
            {
                _diagnosticCanvas.gameObject.SetActive(visible);
            }

            NeonQuestLogger.LogDebug($"Diagnostic UI {(visible ? "shown" : "hidden")}", 
                NeonQuestLogger.LogCategory.General, this);
        }

        private void ClearLogs()
        {
            _logEntries.Clear();
            UpdateLogDisplay();
            NeonQuestLogger.LogInfo("Diagnostic logs cleared", NeonQuestLogger.LogCategory.General, this);
        }

        private void OnDestroy()
        {
            if (_performanceMonitor != null)
            {
                _performanceMonitor.OnMetricsUpdated -= UpdatePerformanceDisplay;
                _performanceMonitor.OnPerformanceWarning -= HandlePerformanceWarning;
                _performanceMonitor.OnPerformanceCritical -= HandlePerformanceCritical;
            }

            Application.logMessageReceived -= HandleLogMessage;
        }
    }
}