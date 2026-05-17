using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using ETABSv1;

namespace Beam_Rebar_Design.Models
{
    public class EtabsConnector
    {
        private cOAPI _etabsObject;
        private cSapModel _sapModel;
        private bool _isConnected;

        public bool IsConnected { get { return _isConnected; } }

        public bool Connect()
        {
            try
            {
                object rawObject = Marshal.GetActiveObject("CSI.ETABS.API.ETABSObject");

                if (rawObject != null)
                {
                    _etabsObject = (cOAPI)rawObject;
                    _sapModel = _etabsObject.SapModel;
                    _isConnected = _sapModel != null;
                    return _isConnected;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi kết nối ETABS: " + ex.Message);
            }

            _isConnected = false;
            return false;
        }

        public string GetModelName()
        {
            if (!_isConnected) return "";
            return _sapModel.GetModelFilename();
        }

        public List<string> GetAllLoadCasesAndCombos()
        {
            List<string> results = new List<string>();
            
            int numCases = 0;
            string[] caseNames = null;
            _sapModel.LoadCases.GetNameList(ref numCases, ref caseNames);
            if (caseNames != null) foreach (string s in caseNames) results.Add(s);

            int numCombos = 0;
            string[] comboNames = null;
            _sapModel.RespCombo.GetNameList(ref numCombos, ref comboNames);
            if (comboNames != null) foreach (string s in comboNames) results.Add(s);

            return results;
        }

        public List<BeamModel> GetAllBeams()
        {
            List<BeamModel> beams = new List<BeamModel>();

            int numFrames = 0;
            string[] frameNames = null;
            _sapModel.FrameObj.GetNameList(ref numFrames, ref frameNames);

            if (frameNames != null)
            {
                foreach (string name in frameNames)
                {
                    string propName = "";
                    string s1 = "", s2 = "";
                    _sapModel.FrameObj.GetSection(name, ref propName, ref s1);
                    _sapModel.FrameObj.GetLabelFromName(name, ref s2, ref s1);

                    string fileName = "", matProp = "", notes = "", guid = "";
                    double h = 0, b = 0;
                    int color = 0;
                    
                    int ret = _sapModel.PropFrame.GetRectangle(propName, ref fileName, ref matProp, ref h, ref b, ref color, ref notes, ref guid);

                    if (ret == 0 && h > 0 && b > 0)
                    {
                        h = h * 1000;
                        b = b * 1000;

                        BeamModel beam = new BeamModel();
                        beam.Name = name;
                        beam.Label = s2;
                        beam.Story = s1;
                        beam.B = b;
                        beam.H = h;
                        beam.Ltt = 0;
                        beams.Add(beam);
                    }
                }
            }
            return beams;
        }

        public List<DetailedFrameForceData> GetFrameForces(string caseName, List<string> frameNames)
        {
            List<DetailedFrameForceData> results = new List<DetailedFrameForceData>();

            // Sửa lỗi: Tên hàm đúng là DeselectAllCasesAndCombosForOutput
            _sapModel.Results.Setup.DeselectAllCasesAndCombosForOutput();
            _sapModel.Results.Setup.SetComboSelectedForOutput(caseName, true);
            _sapModel.Results.Setup.SetCaseSelectedForOutput(caseName, true);

            foreach (string name in frameNames)
            {
                int numResults = 0;
                string[] objNames = null, elmNames = null, loadCases = null, stepTypes = null;
                double[] objSta = null, elmSta = null, stepNum = null, p = null, v2 = null, v3 = null, t = null, m2 = null, m3 = null;

                _sapModel.Results.FrameForce(name, eItemTypeElm.Element, ref numResults, ref objNames, ref objSta, ref elmNames, ref elmSta, ref loadCases, ref stepTypes, ref stepNum, ref p, ref v2, ref v3, ref t, ref m2, ref m3);

                if (objNames != null)
                {
                    for (int i = 0; i < numResults; i++)
                    {
                        DetailedFrameForceData data = new DetailedFrameForceData();
                        data.FrameName = name;
                        data.Station = objSta[i];
                        data.Moment2 = m2[i];
                        data.Moment3 = m3[i];
                        data.Shear2 = v2[i];
                        data.Shear3 = v3[i];
                        data.StepType = stepTypes[i];
                        results.Add(data);
                    }
                }
            }
            return results;
        }
    }
}