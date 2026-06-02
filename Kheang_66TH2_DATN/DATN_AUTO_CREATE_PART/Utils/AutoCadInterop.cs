using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using DATN_AUTO_CREATE_PART.Models;

namespace DATN_AUTO_CREATE_PART.Utils
{
    public static class AutoCadInterop
    {
        // =====================================================
        // LAYER FILTER - Lọc entity theo tên layer
        // =====================================================

        /// <summary>
        /// Parse chuỗi filter "beam, dam, frame" thành danh sách keywords uppercase.
        /// </summary>
        private static string[] ParseLayerKeywords(string layerFilter)
        {
            if (string.IsNullOrWhiteSpace(layerFilter)) return new string[0];
            return layerFilter
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(k => k.Trim().ToUpperInvariant())
                .Where(k => k.Length > 0)
                .ToArray();
        }

        /// <summary>
        /// Kiểm tra tên layer có chứa bất kỳ keyword nào không (case-insensitive).
        /// Nếu không có filter (rỗng), chấp nhận tất cả.
        /// </summary>
        private static bool MatchesLayerFilter(string layerName, string[] keywords)
        {
            if (keywords == null || keywords.Length == 0) return true;
            string upper = (layerName ?? "").ToUpperInvariant();
            return keywords.Any(k => upper.Contains(k));
        }
        public static XyzData GetCadOrigin()
        {
            try
            {
                dynamic a = Marshal.GetActiveObject("AutoCAD.Application");
                a.Visible = true;
                a.WindowState = 3; // Maximized
                
                dynamic doc = a.ActiveDocument;
                try { doc.Activate(); } catch { }
                
                WindowFocusHelper.BringToFront("acad");
                
                var pointCad = doc.Utility.GetPoint(Type.Missing, "\nSelect CAD Origin Point (once for project): ");
                var originObj = ((IEnumerable)pointCad).Cast<object>().Select(x => x.ToString()).ToArray();
                return new XyzData(Convert.ToDouble(originObj[0]), Convert.ToDouble(originObj[1]), Convert.ToDouble(originObj[2]));
            }
            catch
            {
                return null;
            }
        }

        public static void ExtractBeams(out List<CadBeams> extractedBeams, XyzData originPoint, string layerFilter = "")
        {
            extractedBeams = new List<CadBeams>();
            var keywords = ParseLayerKeywords(layerFilter);

            try
            {
                dynamic a = Marshal.GetActiveObject("AutoCAD.Application");
                a.Visible = true;
                a.WindowState = 3;
                
                try { a.ActiveDocument.Activate(); } catch { }

                dynamic doc = a.ActiveDocument;

                var newset = doc.SelectionSets.Add(Guid.NewGuid().ToString());
                newset.SelectOnScreen();

                List<dynamic> listText = new List<dynamic>();
                List<dynamic> listLine = new List<dynamic>();

                foreach (dynamic s in newset)
                {
                    if (s.EntityName == "AcDbText" || s.EntityName == "AcDbMText") 
                    {
                        string t = CleanCadText(s.TextString);
                        listText.Add(s);
                        continue;
                    }

                    if (!MatchesLayerFilter((string)s.Layer, keywords)) continue;

                    if (s.EntityName == "AcDbLine") listLine.Add(s);
                }

                List<TextData> listpoint = new List<TextData>();
                foreach (var text in listText)
                {
                    try
                    {
                        string[] arrtextpoint = ((IEnumerable)text.InsertionPoint).Cast<object>().Select(x => x.ToString()).ToArray();
                        listpoint.Add(new TextData()
                        {
                            Point = new XyzData(Convert.ToDouble(arrtextpoint[0]), Convert.ToDouble(arrtextpoint[1]), Convert.ToDouble(arrtextpoint[2])),
                            Text = text.TextString
                        });
                    } catch { }
                }

                // Lớp phụ để hỗ trợ tính toán đường song song
                var parsedLines = new List<dynamic>();
                foreach (var line in listLine)
                {
                    try
                    {
                        dynamic sp = line.StartPoint;
                        dynamic ep = line.EndPoint;
                        XyzData p1 = new XyzData((double)sp[0], (double)sp[1], 0);
                        XyzData p2 = new XyzData((double)ep[0], (double)ep[1], 0);
                        double length = p1.DistanceTo(p2);
                        if (length < 10) continue; // Bỏ qua nét quá ngắn
                        
                        double dx = (p2.X - p1.X) / length;
                        double dy = (p2.Y - p1.Y) / length;
                        
                        parsedLines.Add(new 
                        { 
                            Start = p1, 
                            End = p2, 
                            Length = length,
                            Mid = new XyzData((p1.X + p2.X) / 2, (p1.Y + p2.Y) / 2, 0),
                            DirX = dx,
                            DirY = dy
                        });
                    }
                    catch { }
                }

                var usedLines = new HashSet<int>();
                for (int i = 0; i < parsedLines.Count; i++)
                {
                    if (usedLines.Contains(i)) continue;
                    var l1 = parsedLines[i];
                    
                    int bestMatchIdx = -1;
                    double minCenterDist = double.MaxValue;

                    for (int j = i + 1; j < parsedLines.Count; j++)
                    {
                        if (usedLines.Contains(j)) continue;
                        var l2 = parsedLines[j];

                        // Kiểm tra chiều dài bằng nhau (cho phép sai số nhỏ)
                        if (Math.Abs(l1.Length - l2.Length) > 10.0) continue;

                        // Kiểm tra song song (Tích vô hướng của 2 vector hướng ~ 1 hoặc -1)
                        double dot = l1.DirX * l2.DirX + l1.DirY * l2.DirY;
                        if (Math.Abs(Math.Abs(dot) - 1.0) > 0.05) continue; // Không song song

                        // Kiểm tra tính đối xứng (vector nối 2 trung điểm phải vuông góc với hướng dầm)
                        double dx = l2.Mid.X - l1.Mid.X;
                        double dy = l2.Mid.Y - l1.Mid.Y;
                        double midDist = Math.Sqrt(dx * dx + dy * dy);

                        if (midDist < 50 || midDist > 2000) continue; // Dầm thường rộng từ 50 đến 2000mm

                        double midDot = Math.Abs((dx * l1.DirX + dy * l1.DirY) / midDist);
                        if (midDot > 0.2) continue; // Không vuông góc

                        if (midDist < minCenterDist)
                        {
                            minCenterDist = midDist;
                            bestMatchIdx = j;
                        }
                    }

                    if (bestMatchIdx != -1)
                    {
                        var l2 = parsedLines[bestMatchIdx];
                        usedLines.Add(i);
                        usedLines.Add(bestMatchIdx);

                        XyzData centerStart, centerEnd;
                        // Xác định 2 đường có cùng chiều vẽ hay ngược chiều
                        double distSS = l1.Start.DistanceTo(l2.Start);
                        double distSE = l1.Start.DistanceTo(l2.End);

                        if (distSS < distSE)
                        {
                            centerStart = new XyzData((l1.Start.X + l2.Start.X) / 2, (l1.Start.Y + l2.Start.Y) / 2, 0);
                            centerEnd = new XyzData((l1.End.X + l2.End.X) / 2, (l1.End.Y + l2.End.Y) / 2, 0);
                        }
                        else
                        {
                            centerStart = new XyzData((l1.Start.X + l2.End.X) / 2, (l1.Start.Y + l2.End.Y) / 2, 0);
                            centerEnd = new XyzData((l1.End.X + l2.Start.X) / 2, (l1.End.Y + l2.Start.Y) / 2, 0);
                        }

                        // Tìm Text (tag) gần nhất với trung tâm dầm
                        XyzData beamCenter = new XyzData((centerStart.X + centerEnd.X) / 2, (centerStart.Y + centerEnd.Y) / 2, 0);
                        string beamText = "Undefined";
                        if (listpoint.Count > 0)
                        {
                            var textData = listpoint.OrderBy(x => x.Point.DistanceTo(beamCenter)).FirstOrDefault();
                            if (textData != null && textData.Point.DistanceTo(beamCenter) < 2000)
                            {
                                beamText = textData.Text;
                            }
                        }

                        extractedBeams.Add(new CadBeams()
                        {
                            StartPoint = centerStart,
                            EndPoint = centerEnd,
                            Text = beamText
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("AutoCAD Error: " + ex.Message);
            }
        }

        public static void ExtractColumns(out List<CadRectangle> extractedColumns, XyzData originPoint, string layerFilter)
        {
            extractedColumns = new List<CadRectangle>();
            var keywords = ParseLayerKeywords(layerFilter);

            try
            {
                dynamic a = Marshal.GetActiveObject("AutoCAD.Application");
                a.Visible = true;
                a.WindowState = 3;
                
                try { a.ActiveDocument.Activate(); } catch { }

                dynamic doc = a.ActiveDocument;

                var newset = doc.SelectionSets.Add(Guid.NewGuid().ToString());
                newset.SelectOnScreen();

                List<dynamic> listPolyline = new List<dynamic>();
                List<TextData> listText = new List<TextData>();

                foreach (dynamic s in newset)
                {
                    if (s.EntityName == "AcDbText" || s.EntityName == "AcDbMText")
                    {
                        string t = CleanCadText(s.TextString);
                        // Optional: only pick text that looks like a column label or dimension
                        double[] ins = ((IEnumerable)s.InsertionPoint).Cast<double>().ToArray();
                        listText.Add(new TextData { Point = new XyzData(ins[0], ins[1], ins[2]), Text = t });
                        continue;
                    }

                    String layerName = (String)s.Layer;

                    if (!MatchesLayerFilter(layerName, keywords)) continue;
                    
                    if (s.EntityName == "AcDbPolyline") listPolyline.Add(s);
                }

                foreach (var polyline in listPolyline)
                {
                    try
                    {
                        object objCoords = polyline.Coordinates;
                        double[] c = null;
                        if (objCoords is double[] da) c = da;
                        else if (objCoords is IEnumerable ie) c = ie.Cast<object>().Select(Convert.ToDouble).ToArray();

                        if (c != null && c.Length >= 8) // At least 4 points * 2 (X,Y)
                        {
                            var pts = new List<XyzData>();
                            for (int i = 0; i < c.Length / 2; i++)
                            {
                                pts.Add(new XyzData(c[i * 2], c[i * 2 + 1], 0));
                            }

                            string mask = "";
                            if (listText.Count > 0)
                            {
                                var center = new XyzData(pts.Average(p => p.X), pts.Average(p => p.Y), 0);
                                var closestText = listText.OrderBy(t => t.Point.DistanceTo(center)).FirstOrDefault();
                                if (closestText != null && closestText.Point.DistanceTo(center) < 1000) // 1m threshold
                                {
                                    mask = closestText.Text;
                                }
                            }

                            extractedColumns.Add(new CadRectangle()
                            {
                                P1 = pts[0],
                                P2 = pts[1],
                                P3 = pts[2],
                                P4 = pts[3],
                                Mask = mask
                            });
                        }
                    } catch { }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("AutoCAD Error: " + ex.Message);
            }
        }

        public static void ExtractFloors(out List<CadFloor> extractedFloors, XyzData originPoint, string layerFilter = "")
        {
            extractedFloors = new List<CadFloor>();
            var keywords = ParseLayerKeywords(layerFilter);

            try
            {
                dynamic a = Marshal.GetActiveObject("AutoCAD.Application");
                a.Visible = true;
                a.WindowState = 3;
                try { a.ActiveDocument.Activate(); } catch { }

                dynamic doc = a.ActiveDocument;

                var newset = doc.SelectionSets.Add(Guid.NewGuid().ToString());
                newset.SelectOnScreen();

                List<dynamic> listPolylines = new List<dynamic>();

                foreach (dynamic s in newset)
                {
                    if (!MatchesLayerFilter((string)s.Layer, keywords)) continue;
                    if (s.EntityName == "AcDbPolyline") listPolylines.Add(s);
                }

                foreach (var polyline in listPolylines)
                {
                    try
                    {
                        dynamic c = polyline.Coordinates;
                        int vCount = ((IEnumerable)c).Cast<object>().Count() / 2;
                        var pts = new List<XyzData>();
                        for (int j = 0; j < vCount; j++)
                        {
                            pts.Add(new XyzData((double)c[j * 2], (double)c[j * 2 + 1], 0));
                        }

                        // Filter redundant points logic from ProjectApp
                        for (int item = 0; item < pts.Count; item++)
                        {
                            for (int item1 = pts.Count - 1; item1 > item; item1--)
                            {
                                if (pts[item].DistanceTo(pts[item1]) < 0.08)
                                {
                                    pts.RemoveAt(item1);
                                }
                            }
                        }

                        double area = 0;
                        try { area = polyline.Area / 1000000.0; } catch { } // Area in m2 approx

                        extractedFloors.Add(new CadFloor()
                        {
                            Points = pts,
                            Area = Math.Round(area, 2)
                        });
                    } catch { }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("AutoCAD Error: " + ex.Message);
            }
        }

        public static void ExtractGrids(out string coordX, out string coordY, out string labelX, out string labelY, XyzData originPoint, string layerFilter = "")
        {
            coordX = "0"; coordY = "0"; labelX = "1"; labelY = "A";
            var keywords = ParseLayerKeywords(layerFilter);

            try
            {
                dynamic a = Marshal.GetActiveObject("AutoCAD.Application");
                a.Visible = true;
                a.WindowState = 3;
                try { a.ActiveDocument.Activate(); } catch { }

                dynamic doc = a.ActiveDocument;

                if (originPoint != null)
                {
                    double[] origin = new double[] { originPoint.X, originPoint.Y };
                    var newset = doc.SelectionSets.Add(Guid.NewGuid().ToString());
                    newset.SelectOnScreen();

                    List<dynamic> lines = new List<dynamic>();
                    var texts = new List<(double[] ins, string text)>();

                    foreach (dynamic s in newset)
                    {
                        if (s.EntityName == "AcDbText" || s.EntityName == "AcDbMText") 
                        {
                            double[] ins = ((IEnumerable)s.InsertionPoint).Cast<double>().ToArray();
                            texts.Add((ins, CleanCadText(s.TextString)));
                            continue;
                        }

                        if (!MatchesLayerFilter((string)s.Layer, keywords)) continue;

                        if (s.EntityName == "AcDbLine") 
                        {
                            lines.Add(s);
                        }
                        else if (s.EntityName == "AcDbBlockReference")
                        {
                            if (s.HasAttributes)
                            {
                                double[] ins = ((IEnumerable)s.InsertionPoint).Cast<double>().ToArray();
                                foreach (dynamic att in s.GetAttributes())
                                {
                                    if (!string.IsNullOrWhiteSpace(att.TextString))
                                    {
                                        texts.Add((ins, CleanCadText(att.TextString)));
                                        break;
                                    }
                                }
                            }
                        }
                    }

                    var xAxes = new List<(double pos, string text)>();
                    var yAxes = new List<(double pos, string text)>();

                    foreach (var line in lines)
                    {
                        double[] p1 = ((IEnumerable)line.StartPoint).Cast<double>().ToArray();
                        double[] p2 = ((IEnumerable)line.EndPoint).Cast<double>().ToArray();

                        bool isVertical = Math.Abs(p1[0] - p2[0]) < 10.0;
                        bool isHorizontal = Math.Abs(p1[1] - p2[1]) < 10.0;

                        if (isVertical)
                        {
                            double xPos = p1[0] - origin[0];
                            string matchedText = FindClosestText(texts, p1, p2, isVertical);
                            xAxes.Add((xPos, matchedText));
                        }
                        else if (isHorizontal)
                        {
                            double yPos = p1[1] - origin[1];
                            string matchedText = FindClosestText(texts, p1, p2, isVertical);
                            yAxes.Add((yPos, matchedText));
                        }
                    }

                    xAxes = xAxes.OrderBy(x => x.pos).Where(x => x.pos >= -10.0).ToList();
                    yAxes = yAxes.OrderBy(y => y.pos).Where(y => y.pos >= -10.0).ToList();

                    if (xAxes.Any())
                    {
                        coordX = BuildTeklaSpacing(xAxes.Select(x => x.pos).ToList());
                        labelX = string.Join(" ", xAxes.Select(x => string.IsNullOrEmpty(x.text) ? "?" : x.text));
                    }
                    if (yAxes.Any())
                    {
                        coordY = BuildTeklaSpacing(yAxes.Select(y => y.pos).ToList());
                        labelY = string.Join(" ", yAxes.Select(y => string.IsNullOrEmpty(y.text) ? "?" : y.text));
                    }
                }
            }
            catch { }
        }

        private static string FindClosestText(List<(double[] ins, string text)> texts, double[] p1, double[] p2, bool isVertical)
        {
            try
            {
                string bestText = "";
                double minD = double.MaxValue; // Unlimited threshold - simply find absolute closest selected label
                foreach (var txt in texts)
                {
                    double[] ins = txt.ins;
                    double d1 = Math.Sqrt(Math.Pow(ins[0] - p1[0], 2) + Math.Pow(ins[1] - p1[1], 2));
                    double d2 = Math.Sqrt(Math.Pow(ins[0] - p2[0], 2) + Math.Pow(ins[1] - p2[1], 2));
                    double min = Math.Min(d1, d2);
                    if (min < minD)
                    {
                        minD = min;
                        bestText = txt.text;
                    }
                }
                return string.IsNullOrWhiteSpace(bestText) ? "?" : bestText.Replace(" ", ""); // Tekla needs space-separated array, prevent internal spaces
            } catch { return "?"; }
        }

        private static string CleanCadText(string rawText)
        {
            if (string.IsNullOrWhiteSpace(rawText)) return "";
            string cleaned = rawText;
            // Strip MText formatting sequences e.g. "{\\fArial|...;Text}"
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\\\\[A-Za-z0-9~\|]+[^;]*;", "");
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\\[A-Za-z0-9~\|]+[^;]*;", "");
            cleaned = cleaned.Replace("{", "").Replace("}", "");
            return cleaned.Trim();
        }

        private static string BuildTeklaSpacing(List<double> absolutePositions)
        {
            if (absolutePositions.Count == 0) return "0";
            List<string> spaces = new List<string> { "0" };
            double shift = absolutePositions[0]; // shift all to start at 0
            
            for (int i = 1; i < absolutePositions.Count; i++)
            {
                double space = absolutePositions[i] - absolutePositions[i - 1];
                spaces.Add(space.ToString("F0"));
            }
            return string.Join(" ", spaces);
        }
    }
}
