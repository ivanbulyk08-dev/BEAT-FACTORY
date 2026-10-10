using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeatFactory
{
    /// <summary>
    /// Results card used after concerts and tours: header, big title, attendance, reward rows with count-up numbers,
    /// progress bars (popularity / concert XP), status chips and a COLLECT button.
    /// </summary>
    public class RewardPanel
    {
        public RectTransform root;
        public CanvasGroup group;
        public TextMeshProUGUI header, title, subtitle, attendance, attendanceLabel, chips;
        public Image glow;
        public Button collect;
        public TextMeshProUGUI collectLabel;
        readonly List<Row> rows = new List<Row>();
        public Image popBar, popCap, xpBar;
        public TextMeshProUGUI popText, xpText;

        class Row
        {
            public TextMeshProUGUI value, detail;
            public double target;
            public Func<double, string> format;
        }

        public static RewardPanel Build(RectTransform parent, string collectText)
        {
            var s = UIKit.Skin;
            var p = new RewardPanel();
            p.root = UIKit.Rect("Results", parent);
            // full width with margins (narrow phones), fixed height from the bottom
            p.root.anchorMin = new Vector2(0, 0);
            p.root.anchorMax = new Vector2(1, 0);
            p.root.pivot = new Vector2(0.5f, 0);
            p.root.offsetMin = new Vector2(28, 40);
            p.root.offsetMax = new Vector2(-28, 40 + 1360);
            p.group = p.root.gameObject.AddComponent<CanvasGroup>();
            p.glow = UIKit.Image("Glow", p.root, new Color(1, 0.3f, 0.8f, 0.2f), s.softGlow, true);
            UIKit.Stretch(p.glow.rectTransform, -50, -50, -50, -50);
            var card = UIKit.Card("Card", p.root, s.card);
            UIKit.Stretch(card.rectTransform);
            card.raycastTarget = true;
            var r = p.root;

            p.header = UIKit.Text("Header", r, "CONCERT COMPLETE", 34, s.gold, s.heading, TextAlignmentOptions.Center, 12);
            UIKit.TopRow(p.header.rectTransform, 40, 50, 40, 40);
            p.title = UIKit.Text("Title", r, "ARTIST", 104, s.text, s.display, TextAlignmentOptions.Center, 3);
            UIKit.TopRow(p.title.rectTransform, 92, 120, 40, 40);
            p.subtitle = UIKit.Text("Subtitle", r, "ARENA · CITY", 32, s.textDim, s.heading, TextAlignmentOptions.Center, 6);
            UIKit.TopRow(p.subtitle.rectTransform, 214, 46, 40, 40);

            var att = UIKit.Rounded("AttendanceBox", r, new Color(0, 0, 0, 0.3f));
            UIKit.TopRow(att.rectTransform, 280, 170, 50, 50);
            p.attendance = UIKit.Text("Attendance", att.rectTransform, "0 / 0", 70, s.text, s.display, TextAlignmentOptions.Center, 2);
            UIKit.Stretch(p.attendance.rectTransform, 20, 20, 14, 70);
            p.attendanceLabel = UIKit.Text("AttendanceLabel", att.rectTransform, "SOLD OUT", 32, s.fame, s.heading, TextAlignmentOptions.Center, 8);
            UIKit.Stretch(p.attendanceLabel.rectTransform, 20, 20, 104, 18);

            p.chips = UIKit.Text("Chips", r, "", 30, s.cyan, s.heading, TextAlignmentOptions.Center, 4);
            UIKit.TopRow(p.chips.rectTransform, 1080, 80, 40, 40);
            p.chips.textWrappingMode = TextWrappingModes.Normal;

            p.collect = UIKit.Button("CollectButton", r, s.money, collectText, 56, out p.collectLabel);
            var crt = (RectTransform)p.collect.transform;
            crt.anchorMin = new Vector2(0.1f, 0);
            crt.anchorMax = new Vector2(0.9f, 0);
            crt.pivot = new Vector2(0.5f, 0);
            crt.offsetMin = new Vector2(0, 40);
            crt.offsetMax = new Vector2(0, 190);
            return p;
        }

        /// <summary>Adds a reward row: label · big value (count-up) · optional detail line. Rows stack from y = 480.</summary>
        public void AddRow(string label, Color color, double value, Func<double, string> format, string detail = null)
        {
            var s = UIKit.Skin;
            float y = 480 + rows.Count * 150;
            var row = UIKit.Rect("Row_" + label, root);
            UIKit.TopRow(row, y, 140, 50, 50);
            var line = UIKit.Image("Line", row, new Color(1, 1, 1, 0.06f));
            UIKit.TopRow(line.rectTransform, 0, 2);
            var l = UIKit.Text("Label", row, label, 30, s.textDim, s.heading, TextAlignmentOptions.TopLeft, 6);
            UIKit.Stretch(l.rectTransform, 0, 0, 22, 60);
            var v = UIKit.Text("Value", row, format(0), 66, color, s.display, TextAlignmentOptions.TopRight, 1);
            UIKit.Stretch(v.rectTransform, 260, 0, 14, 40);
            var d = UIKit.Text("Detail", row, detail ?? "", 24, new Color(s.textDim.r, s.textDim.g, s.textDim.b, 0.8f), s.body, TextAlignmentOptions.BottomRight);
            UIKit.Stretch(d.rectTransform, 0, 0, 90, 6);
            rows.Add(new Row { value = v, target = value, format = format, detail = d });
        }

        /// <summary>Bar under the last row (popularity with the studio limit, or XP).</summary>
        public Image AddBar(Color color, out TextMeshProUGUI text, out Image capMarker)
        {
            var s = UIKit.Skin;
            float y = 480 + rows.Count * 150 - 34;
            var fill = UIKit.Bar("Bar", root, color, out var track);
            UIKit.TopRow(track.rectTransform, y, 18, 60, 360);
            text = UIKit.Text("BarText", root, "", 22, s.textDim, s.body, TextAlignmentOptions.Left);
            UIKit.TopRow(text.rectTransform, y - 8, 34, 660, 60);
            capMarker = UIKit.Image("Cap", track.rectTransform, Color.white);
            capMarker.rectTransform.anchorMin = capMarker.rectTransform.anchorMax = new Vector2(1, 0.5f);
            capMarker.rectTransform.sizeDelta = new Vector2(4, 30);
            return fill;
        }

        public void Clear()
        {
            foreach (var r in rows) UnityEngine.Object.Destroy(r.value.transform.parent.gameObject);
            rows.Clear();
            foreach (Transform t in root) if (t.name == "Bar" || t.name == "BarText") UnityEngine.Object.Destroy(t.gameObject);
        }

        /// <summary>Counts every row up one after another (with a tick sound).</summary>
        public IEnumerator CountUp(float perRow = 0.55f)
        {
            foreach (var r in rows)
            {
                AudioManager.Play(SoundId.Money, 1f + rows.IndexOf(r) * 0.08f);
                for (float t = 0; t < 1f; t += Time.unscaledDeltaTime / perRow)
                {
                    float e = 1f - Mathf.Pow(1f - t, 3f);
                    r.value.text = r.format(r.target * e);
                    r.value.transform.localScale = Vector3.one * (1f + 0.08f * Mathf.Sin(t * Mathf.PI));
                    yield return null;
                }
                r.value.text = r.format(r.target);
                r.value.transform.localScale = Vector3.one;
            }
        }

        public void ShowFinal()
        {
            foreach (var r in rows) r.value.text = r.format(r.target);
        }

        public static IEnumerator SlideIn(RectTransform rt, CanvasGroup g, float duration = 0.45f)
        {
            var end = rt.anchoredPosition;
            var start = end + new Vector2(0, -400);
            for (float t = 0; t < 1f; t += Time.unscaledDeltaTime / duration)
            {
                float e = 1f + 2.70158f * Mathf.Pow(t - 1f, 3) + 1.70158f * Mathf.Pow(t - 1f, 2);
                rt.anchoredPosition = Vector2.LerpUnclamped(start, end, e);
                g.alpha = Mathf.Clamp01(t * 2.5f);
                yield return null;
            }
            rt.anchoredPosition = end;
            g.alpha = 1f;
        }

        public static string Money(double v) => "+" + NumberFormatter.FormatMoney(v);
        public static string Plain(double v) => "+" + NumberFormatter.Format(Math.Round(v));
        public static string Percent(double v) => "+" + NumberFormatter.Percent(v, "0.0");
        public static string Int(double v) => "+" + NumberFormatter.Group(Math.Round(v));
    }
}
