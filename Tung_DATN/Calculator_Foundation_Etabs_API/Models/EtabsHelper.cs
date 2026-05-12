using ETABSv1;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Calculator_Foundation_Etabs_API.Models
{
    public class EtabsHelper
    {
        private class FrameData
        {
            public string Name { get; set; }
            public double X1 { get; set; }
            public double Y1 { get; set; }
            public double Z1 { get; set; }
            public double X2 { get; set; }
            public double Y2 { get; set; }
            public double Z2 { get; set; }

            public bool IsHorizontal => Math.Abs(X1 - X2) > Math.Abs(Y1 - Y2); 
            
            public double AlignmentValue => IsHorizontal ? (Y1 + Y2) / 2.0 : (X1 + X2) / 2.0;

            public double SortValue => IsHorizontal ? Math.Min(X1, X2) : Math.Min(Y1, Y2);
        }

        public static List<string> GetLoadCombinations()
        {
            var list = new List<string>();
            try
            {
                cOAPI myETABSObject = (cOAPI)System.Runtime.InteropServices.Marshal.GetActiveObject("CSI.ETABS.API.ETABSObject");
                cSapModel mySapModel = myETABSObject.SapModel;

                int numberNames = 0;
                string[] myName = null;
                if (mySapModel.RespCombo.GetNameList(ref numberNames, ref myName) == 0 && numberNames > 0)
                {
                    list.AddRange(myName);
                }
            }
            catch (Exception) { }
            return list;
        }

        public static List<RawStripData> GetBaseLevelFoundations(string loadCombo)
        {
            var resultList = new List<RawStripData>();
            try
            {
                cOAPI myETABSObject = (cOAPI)System.Runtime.InteropServices.Marshal.GetActiveObject("CSI.ETABS.API.ETABSObject");
                cSapModel mySapModel = myETABSObject.SapModel;

                mySapModel.SetPresentUnits(eUnits.kN_m_C);

                int numFrames = 0;
                string[] frameNames = null;
                mySapModel.FrameObj.GetNameList(ref numFrames, ref frameNames);
                if (numFrames == 0) return resultList;

                var allFramesData = new List<FrameData>();
                double baseZ = double.MaxValue;

                for (int i = 0; i < numFrames; i++)
                {
                    string fName = frameNames[i];
                    string p1 = "", p2 = "";
                    mySapModel.FrameObj.GetPoints(fName, ref p1, ref p2);
                    double x1 = 0, y1 = 0, z1 = 0, x2 = 0, y2 = 0, z2 = 0;
                    mySapModel.PointObj.GetCoordCartesian(p1, ref x1, ref y1, ref z1);
                    mySapModel.PointObj.GetCoordCartesian(p2, ref x2, ref y2, ref z2);

                    allFramesData.Add(new FrameData { Name = fName, X1 = x1, Y1 = y1, Z1 = z1, X2 = x2, Y2 = y2, Z2 = z2 });
                    if (z1 < baseZ) baseZ = z1;
                    if (z2 < baseZ) baseZ = z2;
                }

                var baseFrames = allFramesData.Where(f => Math.Abs(f.Z1 - baseZ) < 0.001 && Math.Abs(f.Z2 - baseZ) < 0.001).ToList();
                var groupedFrames = new List<List<FrameData>>();
                double tolerance = 0.5;

                foreach (var frame in baseFrames)
                {
                    var group = groupedFrames.FirstOrDefault(g => g[0].IsHorizontal == frame.IsHorizontal && Math.Abs(frame.AlignmentValue - g[0].AlignmentValue) < tolerance);
                    if (group != null) group.Add(frame);
                    else groupedFrames.Add(new List<FrameData> { frame });
                }

                mySapModel.Results.Setup.DeselectAllCasesAndCombosForOutput();
                mySapModel.Results.Setup.SetComboSelectedForOutput(loadCombo, true);

                foreach (var group in groupedFrames)
                {
                    var sortedGroup = group.OrderBy(f => f.SortValue).ToList();
                    string propName = "", sAuto = "";
                    mySapModel.FrameObj.GetSection(sortedGroup[0].Name, ref propName, ref sAuto);
                    
                    double B = 0, H = 0, bw = 0.22;
                    string fileName = "", matProp = "", notes = "", guid = ""; int color = 0;
                    double t3 = 0, t2 = 0;
                    if (mySapModel.PropFrame.GetRectangle(propName, ref fileName, ref matProp, ref t3, ref t2, ref color, ref notes, ref guid) == 0) { B = t2; H = t3; }
                    else {
                        double tf = 0, tw = 0;
                        if (mySapModel.PropFrame.GetTee(propName, ref fileName, ref matProp, ref t3, ref t2, ref tf, ref tw, ref color, ref notes, ref guid) == 0) { B = t2; H = t3; bw = tw; }
                    }

                    var rawStrip = new RawStripData {
                        Name = $"Trục {(sortedGroup[0].IsHorizontal ? "Y=" : "X=")}{Math.Round(sortedGroup[0].AlignmentValue, 2)} ({string.Join("-", sortedGroup.Select(f => f.Name))})",
                        IsHorizontal = sortedGroup[0].IsHorizontal,
                        AlignmentValue = sortedGroup[0].AlignmentValue,
                        MinCoord = sortedGroup.Min(f => f.SortValue),
                        MaxCoord = sortedGroup.Max(f => sortedGroup[0].IsHorizontal ? Math.Max(f.X1, f.X2) : Math.Max(f.Y1, f.Y2)),
                        B = B, H = H, Bw = bw
                    };

                    var jointSet = new HashSet<string>();
                    foreach (var f in sortedGroup) {
                        string pt1 = "", pt2 = "";
                        mySapModel.FrameObj.GetPoints(f.Name, ref pt1, ref pt2);
                        jointSet.Add(pt1); jointSet.Add(pt2);
                    }

                    foreach (var col in allFramesData)
                    {
                        if (Math.Abs(col.Z2 - col.Z1) < 0.1) continue;
                        string colPt1 = "", colPt2 = "";
                        mySapModel.FrameObj.GetPoints(col.Name, ref colPt1, ref colPt2);
                        string basePoint = (col.Z1 <= col.Z2) ? colPt1 : colPt2;
                        if (Math.Abs(Math.Min(col.Z1, col.Z2) - baseZ) > 0.01 || !jointSet.Contains(basePoint)) continue;

                        int nRes = 0; string[] oRes = null, eRes = null, lcRes = null, stRes = null; double[] osRes = null, esRes = null, snRes = null, pRes = null, v2Res = null, v3Res = null, tRes = null, m2Res = null, m3Res = null;
                        if (mySapModel.Results.FrameForce(col.Name, eItemTypeElm.ObjectElm, ref nRes, ref oRes, ref osRes, ref eRes, ref esRes, ref lcRes, ref stRes, ref snRes, ref pRes, ref v2Res, ref v3Res, ref tRes, ref m2Res, ref m3Res) == 0 && nRes > 0)
                        {
                            int idx = (col.Z1 <= col.Z2) ? 0 : nRes - 1;
                            double cx = 0, cy = 0, cz = 0;
                            mySapModel.PointObj.GetCoordCartesian(basePoint, ref cx, ref cy, ref cz);
                            rawStrip.ColumnLoads.Add(new RawColumnLoad {
                                Name = col.Name, X = cx, Y = cy, Z = cz,
                                P = pRes[idx], V2 = v2Res[idx], V3 = v3Res[idx], M2 = m2Res[idx], M3 = m3Res[idx]
                            });
                        }
                    }
                    resultList.Add(rawStrip);
                }
                return resultList;
            }
            catch (Exception) { return resultList; }
        }
    }
}
