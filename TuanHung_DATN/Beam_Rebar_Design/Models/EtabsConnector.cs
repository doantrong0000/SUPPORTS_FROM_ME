using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace Beam_Rebar_Design.Models
{
    /// <summary>
    /// Kết nối và lấy dữ liệu trực tiếp từ ETABS đang chạy
    /// </summary>
    public class EtabsConnector
    {
        private dynamic _etabsObject;
        private dynamic _sapModel;
        private bool _isConnected;

        public bool IsConnected => _isConnected;

        /// <summary>
        /// Kết nối đến ETABS đang chạy
        /// </summary>
        public bool Connect()
        {
            try
            {
                // Thử kết nối ETABS qua COM - hỗ trợ nhiều phiên bản
                string[] progIds = new string[]
                {
                    "CSI.ETABS.API.ETABSObject",
                    "CSI.ETABS.API.ETABSv1.Helper"
                };

                foreach (var progId in progIds)
                {
                    try
                    {
                        // Lấy instance ETABS đang chạy
                        _etabsObject = Marshal.GetActiveObject(progId);
                        if (_etabsObject != null)
                        {
                            _sapModel = _etabsObject.SapModel;
                            _isConnected = _sapModel != null;
                            if (_isConnected)
                            {
                                Debug.WriteLine($"Đã kết nối ETABS qua {progId}");
                                return true;
                            }
                        }
                    }
                    catch
                    {
                        continue;
                    }
                }

                // Fallback: Tìm ETABS qua ROT (Running Object Table)
                try
                {
                    var etabsType = Type.GetTypeFromProgID("CSI.ETABS.API.ETABSObject");
                    if (etabsType != null)
                    {
                        _etabsObject = Marshal.GetActiveObject("CSI.ETABS.API.ETABSObject");
                        _sapModel = _etabsObject.SapModel;
                        _isConnected = _sapModel != null;
                        return _isConnected;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Fallback kết nối thất bại: {ex.Message}");
                }

                _isConnected = false;
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi kết nối ETABS: {ex.Message}");
                _isConnected = false;
                return false;
            }
        }

        /// <summary>
        /// Lấy tên model đang mở
        /// </summary>
        public string GetModelName()
        {
            try
            {
                if (!_isConnected) return "";
                string fileName = _sapModel.GetModelFilename();
                return System.IO.Path.GetFileNameWithoutExtension(fileName);
            }
            catch { return ""; }
        }

        /// <summary>
        /// Lấy danh sách tất cả frame objects (dầm)
        /// </summary>
        public List<string> GetAllFrameNames()
        {
            var result = new List<string>();
            try
            {
                if (!_isConnected) return result;

                int numberNames = 0;
                string[] names = null;
                int ret = _sapModel.FrameObj.GetNameList(ref numberNames, ref names);

                if (ret == 0 && names != null)
                {
                    result.AddRange(names);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi lấy danh sách frame: {ex.Message}");
            }
            return result;
        }

        /// <summary>
        /// Lấy danh sách tên dầm (lọc theo loại frame = Beam)
        /// </summary>
        public List<EtabsBeamInfo> GetAllBeams()
        {
            var beams = new List<EtabsBeamInfo>();
            try
            {
                if (!_isConnected) return beams;

                // Đặt đơn vị là kN, m
                _sapModel.SetPresentUnits(6); // kN_m_C

                int numberNames = 0;
                string[] names = null;
                int ret = _sapModel.FrameObj.GetNameList(ref numberNames, ref names);

                if (ret != 0 || names == null) return beams;

                foreach (var frameName in names)
                {
                    try
                    {
                        // Lấy thông tin section
                        string propName = "";
                        string sAuto = "";
                        ret = _sapModel.FrameObj.GetSection(frameName, ref propName, ref sAuto);
                        if (ret != 0) continue;

                        // Lấy label và story
                        string label = "";
                        string story = "";
                        ret = _sapModel.FrameObj.GetLabelFromName(frameName, ref label, ref story);

                        // Lấy chiều dài frame
                        double length = GetFrameLength(frameName);

                        // Lấy kích thước tiết diện
                        double b = 0, h = 0;
                        GetSectionDimensions(propName, ref b, ref h);

                        // Chỉ lấy những frame có tiết diện hợp lệ (là dầm)
                        if (b > 0 && h > 0 && length > 0)
                        {
                            beams.Add(new EtabsBeamInfo
                            {
                                FrameName = frameName,
                                Label = !string.IsNullOrEmpty(label) ? label : frameName,
                                Story = story,
                                SectionName = propName,
                                B = b * 1000,  // m -> mm
                                H = h * 1000,  // m -> mm
                                Length = length * 1000  // m -> mm
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Lỗi xử lý frame {frameName}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi lấy danh sách dầm: {ex.Message}");
            }
            return beams;
        }

        /// <summary>
        /// Lấy kích thước tiết diện (b, h) từ tên section
        /// </summary>
        private void GetSectionDimensions(string propName, ref double b, ref double h)
        {
            try
            {
                // Thử GetRectangle trước (phổ biến cho dầm bê tông)
                string matProp = "";
                double t3 = 0, t2 = 0;
                int color = 0;
                string notes = "", guid = "";

                int ret = _sapModel.PropFrame.GetRectangle(propName, ref matProp, ref t3, ref t2, ref color, ref notes, ref guid);
                if (ret == 0)
                {
                    b = t2; // Width
                    h = t3; // Height (Depth)
                    return;
                }

                // Thử GetTee
                string fileName = "";
                double tf = 0, tw = 0;

                ret = _sapModel.PropFrame.GetTee(propName, ref fileName, ref matProp, ref t3, ref t2, ref tf, ref tw, ref color, ref notes, ref guid);
                if (ret == 0)
                {
                    b = tw; // Web width
                    h = t3; // Height
                    return;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi lấy kích thước section {propName}: {ex.Message}");
            }
        }

        /// <summary>
        /// Lấy chiều dài frame
        /// </summary>
        private double GetFrameLength(string frameName)
        {
            try
            {
                // Lấy 2 điểm đầu cuối
                string point1 = "", point2 = "";
                int ret = _sapModel.FrameObj.GetPoints(frameName, ref point1, ref point2);
                if (ret != 0) return 0;

                double x1 = 0, y1 = 0, z1 = 0;
                double x2 = 0, y2 = 0, z2 = 0;

                _sapModel.PointObj.GetCoordCartesian(point1, ref x1, ref y1, ref z1);
                _sapModel.PointObj.GetCoordCartesian(point2, ref x2, ref y2, ref z2);

                return Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2) + Math.Pow(z2 - z1, 2));
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Lấy danh sách tất cả Load Cases và Combinations
        /// </summary>
        public List<string> GetAllLoadCasesAndCombos()
        {
            var result = new List<string>();
            try
            {
                if (!_isConnected) return result;

                // Lấy Load Cases
                int numberNames = 0;
                string[] names = null;
                int ret = _sapModel.LoadCases.GetNameList(ref numberNames, ref names);
                if (ret == 0 && names != null)
                {
                    result.AddRange(names);
                }

                // Lấy Load Combinations
                numberNames = 0;
                names = null;
                ret = _sapModel.RespCombo.GetNameList(ref numberNames, ref names);
                if (ret == 0 && names != null)
                {
                    result.AddRange(names);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi lấy load cases: {ex.Message}");
            }
            return result;
        }

        /// <summary>
        /// Lấy nội lực (Moment, Shear) cho tất cả dầm theo một Load Case/Combo
        /// </summary>
        public List<DetailedFrameForceData> GetFrameForces(string loadCaseOrCombo, List<string> frameNames)
        {
            var forceDataList = new List<DetailedFrameForceData>();
            try
            {
                if (!_isConnected) return forceDataList;

                // Đặt đơn vị kN, m
                _sapModel.SetPresentUnits(6); // kN_m_C

                // Deselect tất cả trước
                _sapModel.Results.Setup.DeselectAllCasesAndCombosForOutput();

                // Thử select như Load Case trước
                int ret = _sapModel.Results.Setup.SetCaseSelectedForOutput(loadCaseOrCombo);
                if (ret != 0)
                {
                    // Nếu không phải case, thử combo
                    ret = _sapModel.Results.Setup.SetComboSelectedForOutput(loadCaseOrCombo);
                }

                if (ret != 0)
                {
                    Debug.WriteLine($"Không thể select output cho: {loadCaseOrCombo}");
                    return forceDataList;
                }

                // Lấy kết quả cho từng frame
                foreach (var frameName in frameNames)
                {
                    try
                    {
                        int numberResults = 0;
                        string[] obj = null;
                        double[] objSta = null;
                        string[] elm = null;
                        double[] elmSta = null;
                        string[] loadCase = null;
                        string[] stepType = null;
                        double[] stepNum = null;
                        double[] p = null;
                        double[] v2 = null;
                        double[] v3 = null;
                        double[] t = null;
                        double[] m2 = null;
                        double[] m3 = null;

                        ret = _sapModel.Results.FrameForce(
                            frameName,
                            0, // eItemTypeElm.ObjectElm
                            ref numberResults,
                            ref obj, ref objSta,
                            ref elm, ref elmSta,
                            ref loadCase, ref stepType, ref stepNum,
                            ref p, ref v2, ref v3, ref t, ref m2, ref m3);

                        if (ret == 0 && numberResults > 0)
                        {
                            // Lấy chiều dài frame để tính station ratio
                            double frameLength = GetFrameLength(frameName);

                            for (int i = 0; i < numberResults; i++)
                            {
                                forceDataList.Add(new DetailedFrameForceData
                                {
                                    FrameName = frameName,
                                    LoadCase = loadCase[i],
                                    Station = objSta[i],
                                    FrameLength = frameLength,
                                    StepType = stepType != null && i < stepType.Length ? stepType[i] : "",
                                    Shear2 = v2[i],
                                    Shear3 = v3[i],
                                    Moment2 = m2[i],
                                    Moment3 = m3[i]
                                });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Lỗi lấy nội lực frame {frameName}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi lấy nội lực: {ex.Message}");
            }
            return forceDataList;
        }

        /// <summary>
        /// Ngắt kết nối
        /// </summary>
        public void Disconnect()
        {
            try
            {
                if (_sapModel != null)
                {
                    Marshal.ReleaseComObject(_sapModel);
                    _sapModel = null;
                }
                if (_etabsObject != null)
                {
                    Marshal.ReleaseComObject(_etabsObject);
                    _etabsObject = null;
                }
                _isConnected = false;
            }
            catch { }
        }
    }

    /// <summary>
    /// Thông tin dầm từ ETABS
    /// </summary>
    public class EtabsBeamInfo
    {
        public string FrameName { get; set; }
        public string Label { get; set; }
        public string Story { get; set; }
        public string SectionName { get; set; }
        public double B { get; set; }  // mm
        public double H { get; set; }  // mm
        public double Length { get; set; }  // mm
    }
}
