// 该文件由Cursor 自动生成
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace ShadowGame.Prototype
{
    /// <summary>
    /// OnGUI debug panel for real-device testing of Object Interaction prototype.
    /// Shows state, FPS, interaction stats, and allows parameter tuning + CSV export.
    /// </summary>
    public class InteractionDebugPanel : MonoBehaviour
    {
        [SerializeField] private InteractionController _controller;
        [SerializeField] private GridSystem _gridSystem;
        [SerializeField] private InteractionConfig _config;
        [SerializeField] private bool _showPanel = true;

        private GUIStyle _statsStyle;
        private GUIStyle _btnStyle;
        private GUIStyle _btnActiveStyle;
        private GUIStyle _sliderLabelStyle;
        private bool _stylesReady;

        private int _frameCount;
        private float _fpsTimer;
        private float _fps;

        private int _totalDrags;
        private int _totalRotations;
        private int _totalSnaps;
        private int _totalSelections;
        private float _sessionStartTime;

        private string _exportMsg;
        private float _exportMsgTimer;
        private bool _showTuning;
        private bool _showBounds = true;

        private readonly struct InteractionSample
        {
            public readonly float Time;
            public readonly float DeltaMs;
            public readonly float Fps;
            public readonly string State;
            public readonly string Action;
            public readonly Vector3 ObjectPos;
            public readonly float ObjectAngle;

            public InteractionSample(float time, float deltaMs, float fps, string state, string action, Vector3 pos, float angle)
            {
                Time = time; DeltaMs = deltaMs; Fps = fps;
                State = state; Action = action; ObjectPos = pos; ObjectAngle = angle;
            }
        }

        private readonly List<InteractionSample> _samples = new(4096);
        private InteractableObject.State _prevState;

        private void Start()
        {
            _sessionStartTime = Time.unscaledTime;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 120;
        }

        private void Update()
        {
            _frameCount++;
            _fpsTimer += Time.unscaledDeltaTime;
            if (_fpsTimer >= 0.5f)
            {
                _fps = _frameCount / _fpsTimer;
                _frameCount = 0;
                _fpsTimer = 0f;
            }

            TrackStats();
            RecordSample();

            if (_exportMsgTimer > 0f)
                _exportMsgTimer -= Time.unscaledDeltaTime;
        }

        private void TrackStats()
        {
            if (_controller == null || _controller.Selected == null) return;

            var state = _controller.Selected.CurrentState;
            if (state == _prevState) return;

            switch (state)
            {
                case InteractableObject.State.Selected:
                    _totalSelections++;
                    break;
                case InteractableObject.State.Dragging:
                    _totalDrags++;
                    break;
                case InteractableObject.State.Rotating:
                    _totalRotations++;
                    break;
                case InteractableObject.State.Snapping:
                    _totalSnaps++;
                    break;
            }
            _prevState = state;
        }

        private void RecordSample()
        {
            var selected = _controller != null ? _controller.Selected : null;
            string state = selected != null ? selected.CurrentState.ToString() : "None";
            string action = "Idle";
            Vector3 pos = Vector3.zero;
            float angle = 0f;

            if (selected != null)
            {
                pos = selected.transform.position;
                angle = selected.transform.eulerAngles.y;
                action = selected.CurrentState switch
                {
                    InteractableObject.State.Dragging => "Drag",
                    InteractableObject.State.Rotating => "Rotate",
                    InteractableObject.State.Snapping => "Snap",
                    InteractableObject.State.Selected => "Select",
                    _ => "Idle"
                };
            }

            _samples.Add(new InteractionSample(
                Time.unscaledTime - _sessionStartTime,
                Time.unscaledDeltaTime * 1000f,
                _fps,
                state,
                action,
                pos,
                angle
            ));
        }

        private string ExportCSV()
        {
            if (_samples.Count == 0) return "";

            string dir = Path.Combine(Application.persistentDataPath, "InteractionPrototype");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            string filename = $"interaction_{System.DateTime.Now:yyyyMMdd_HHmmss}.csv";
            string path = Path.Combine(dir, filename);

            var sb = new StringBuilder();
            sb.AppendLine("Time,DeltaMs,FPS,State,Action,PosX,PosY,PosZ,AngleY");

            foreach (var s in _samples)
            {
                sb.AppendLine($"{s.Time:F3},{s.DeltaMs:F2},{s.Fps:F1},{s.State},{s.Action},{s.ObjectPos.x:F3},{s.ObjectPos.y:F3},{s.ObjectPos.z:F3},{s.ObjectAngle:F1}");
            }

            File.WriteAllText(path, sb.ToString());
            return path;
        }

        #region OnGUI

        private void EnsureStyles()
        {
            if (_stylesReady) return;
            _stylesReady = true;

            int fontSz = Mathf.Max(14, Screen.height / 50);
            int btnFont = Mathf.Max(16, Screen.height / 40);

            _statsStyle = new GUIStyle
            {
                fontSize = fontSz,
                normal = { textColor = Color.white },
                padding = new RectOffset(8, 8, 4, 4)
            };

            _btnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = btnFont,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            _btnStyle.normal.textColor = Color.white;
            _btnStyle.normal.background = MakeTex(2, 2, new Color(0.2f, 0.2f, 0.2f, 0.85f));
            _btnStyle.active.background = MakeTex(2, 2, new Color(0.4f, 0.4f, 0.4f, 0.9f));
            _btnStyle.padding = new RectOffset(10, 10, 10, 10);

            _btnActiveStyle = new GUIStyle(_btnStyle);
            _btnActiveStyle.normal.textColor = Color.yellow;
            _btnActiveStyle.normal.background = MakeTex(2, 2, new Color(0.1f, 0.35f, 0.1f, 0.9f));

            _sliderLabelStyle = new GUIStyle
            {
                fontSize = fontSz - 2,
                normal = { textColor = new Color(0.9f, 0.9f, 0.7f) },
                padding = new RectOffset(4, 4, 2, 2)
            };
        }

        private void OnGUI()
        {
            if (!_showPanel) return;
            EnsureStyles();

            float sw = Screen.width;
            float sh = Screen.height;

            DrawStats(sw, sh);
            DrawBottomButtons(sw, sh);

            if (_showTuning)
                DrawTuningPanel(sw, sh);

            DrawExportToast(sw, sh);
        }

        private void DrawStats(float sw, float sh)
        {
            var sel = _controller != null ? _controller.Selected : null;
            string selectedName = sel != null ? sel.name : "—";
            string state = sel != null ? sel.CurrentState.ToString() : "—";

            string posInfo = "";
            if (sel != null)
            {
                var p = sel.transform.position;
                float angle = sel.transform.eulerAngles.y;
                posInfo = $"\nPos: ({p.x:F2}, {p.z:F2})  Angle: {angle:F0}°";
            }

            string stats = $"FPS: {_fps:F0}  |  Object: {selectedName}  |  State: {state}{posInfo}\n" +
                           $"Sel: {_totalSelections}  Drag: {_totalDrags}  Rot: {_totalRotations}  Snap: {_totalSnaps}\n" +
                           $"Grid: {_config.gridSize:F2}  RotStep: {_config.rotationStep:F0}°  Samples: {_samples.Count}";

            GUI.Label(new Rect(10, 10, sw * 0.9f, sh * 0.18f), stats, _statsStyle);
        }

        private void DrawBottomButtons(float sw, float sh)
        {
            float btnW = sw * 0.19f;
            float btnH = sh * 0.07f;
            float gap = sw * 0.012f;
            float bottomY = sh - btnH - sh * 0.03f;
            float startX = (sw - (4f * btnW + 3f * gap)) * 0.5f;

            if (GUI.Button(new Rect(startX, bottomY, btnW, btnH),
                _showTuning ? "HIDE TUNE" : "TUNE", _showTuning ? _btnActiveStyle : _btnStyle))
                _showTuning = !_showTuning;

            if (GUI.Button(new Rect(startX + btnW + gap, bottomY, btnW, btnH), "EXPORT CSV", _btnStyle))
            {
                string path = ExportCSV();
                _exportMsg = string.IsNullOrEmpty(path) ? "Export failed" : $"Saved: {path}";
                _exportMsgTimer = 4f;
            }

            if (GUI.Button(new Rect(startX + 2f * (btnW + gap), bottomY, btnW, btnH), "RESET", _btnStyle))
            {
                _totalSelections = _totalDrags = _totalRotations = _totalSnaps = 0;
                _samples.Clear();
                _sessionStartTime = Time.unscaledTime;
            }

            if (GUI.Button(new Rect(startX + 3f * (btnW + gap), bottomY, btnW, btnH),
                _showBounds ? "BOUNDS ON" : "BOUNDS OFF", _showBounds ? _btnActiveStyle : _btnStyle))
                _showBounds = !_showBounds;
        }

        private void DrawTuningPanel(float sw, float sh)
        {
            float panelW = sw * 0.9f;
            float panelH = sh * 0.35f;
            float panelX = (sw - panelW) * 0.5f;
            float panelY = sh * 0.15f;

            GUI.Box(new Rect(panelX, panelY, panelW, panelH), "");

            float y = panelY + 10;
            float labelW = panelW * 0.35f;
            float sliderW = panelW * 0.45f;
            float valueW = panelW * 0.12f;
            float rowH = panelH / 8f;

            DrawSlider(ref _config.gridSize, 0.1f, 0.5f, "Grid Size", panelX, ref y, labelW, sliderW, valueW, rowH);
            DrawSlider(ref _config.rotationStep, 5f, 45f, "Rot Step (°)", panelX, ref y, labelW, sliderW, valueW, rowH);
            DrawSlider(ref _config.snapSpeed, 1f, 8f, "Snap Speed", panelX, ref y, labelW, sliderW, valueW, rowH);
            DrawSlider(ref _config.selectScaleMultiplier, 1.0f, 1.15f, "Select Scale", panelX, ref y, labelW, sliderW, valueW, rowH);
            DrawSlider(ref _config.bounceAmplitude, 0f, 0.08f, "Bounce Amp", panelX, ref y, labelW, sliderW, valueW, rowH);
            DrawSlider(ref _config.fatFingerMarginDp, 0f, 24f, "FatFinger (dp)", panelX, ref y, labelW, sliderW, valueW, rowH);
            DrawSlider(ref _config.reboundOvershoot, 0f, 1f, "Rebound OS", panelX, ref y, labelW, sliderW, valueW, rowH);
        }

        private void DrawSlider(ref float value, float min, float max, string label,
            float panelX, ref float y, float labelW, float sliderW, float valueW, float rowH)
        {
            float x = panelX + 10;
            GUI.Label(new Rect(x, y, labelW, rowH), label, _sliderLabelStyle);
            value = GUI.HorizontalSlider(new Rect(x + labelW, y + rowH * 0.3f, sliderW, rowH * 0.4f), value, min, max);
            GUI.Label(new Rect(x + labelW + sliderW + 5, y, valueW, rowH), value.ToString("F2"), _sliderLabelStyle);
            y += rowH;
        }

        private void DrawExportToast(float sw, float sh)
        {
            if (_exportMsgTimer <= 0f || string.IsNullOrEmpty(_exportMsg)) return;

            var toastStyle = new GUIStyle(_statsStyle)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.green }
            };
            float tw = sw * 0.8f;
            float btnH = sh * 0.07f;
            float bottomY = sh - btnH - sh * 0.03f;
            GUI.Label(new Rect((sw - tw) * 0.5f, bottomY - btnH - 10, tw, btnH), _exportMsg, toastStyle);
        }

        #endregion

        private Material _glMaterial;

        private void EnsureGLMaterial()
        {
            if (_glMaterial != null) return;
            _glMaterial = new Material(Shader.Find("Hidden/Internal-Colored"))
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            _glMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            _glMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            _glMaterial.SetInt("_Cull", 0);
            _glMaterial.SetInt("_ZWrite", 0);
            _glMaterial.SetInt("_ZTest", 0);
        }

        private void OnRenderObject()
        {
            if (!_showBounds || _gridSystem == null) return;
            EnsureGLMaterial();

            _glMaterial.SetPass(0);
            GL.PushMatrix();
            GL.MultMatrix(Matrix4x4.identity);
            GL.Begin(GL.LINES);

            float y = 0.02f;
            var bMin = _gridSystem.BoundsMin;
            var bMax = _gridSystem.BoundsMax;

            GL.Color(new Color(1f, 1f, 0f, 0.6f));
            GL.Vertex3(bMin.x, y, bMin.y); GL.Vertex3(bMax.x, y, bMin.y);
            GL.Vertex3(bMax.x, y, bMin.y); GL.Vertex3(bMax.x, y, bMax.y);
            GL.Vertex3(bMax.x, y, bMax.y); GL.Vertex3(bMin.x, y, bMax.y);
            GL.Vertex3(bMin.x, y, bMax.y); GL.Vertex3(bMin.x, y, bMin.y);

            GL.End();
            GL.PopMatrix();
        }

        private static Texture2D MakeTex(int w, int h, Color col)
        {
            var pixels = new Color[w * h];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = col;
            var tex = new Texture2D(w, h);
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }
    }
}
