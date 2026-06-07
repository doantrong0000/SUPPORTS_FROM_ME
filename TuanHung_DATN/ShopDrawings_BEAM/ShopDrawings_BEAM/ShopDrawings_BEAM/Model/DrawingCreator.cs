using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using ShopDrawings_BEAM.ViewModels;

namespace ShopDrawings_BEAM.Model
{
    public static class DrawingCreator
    {
        /// <summary>
        /// Tạo toàn bộ bản vẽ bao gồm Sheet, Mặt cắt dọc và Mặt cắt ngang
        /// </summary>
        public static void CreateBeamDrawings(MainViewModel viewModel)
        {
            Document doc = viewModel.Doc;
            if (viewModel.SelectedBeams == null || viewModel.SelectedBeams.Count == 0)
            {
                throw new Exception("Không có cấu kiện dầm nào được chọn!");
            }

            if (viewModel.SelectedTitleBlock == null)
            {
                throw new Exception("Chưa chọn Khung tên!");
            }

            if (viewModel.SelectedSectionViewType == null)
            {
                throw new Exception("Chưa chọn Section View Type!");
            }

            if (viewModel.SelectedDetailViewType == null)
            {
                throw new Exception("Chưa chọn Detail View Type!");
            }

            // Phân tích tỷ lệ bản vẽ
            double scaleValue = 50;
            if (!string.IsNullOrEmpty(viewModel.SelectedViewScale) && viewModel.SelectedViewScale.Contains(":"))
            {
                double.TryParse(viewModel.SelectedViewScale.Split(':')[1].Trim(), out scaleValue);
            }

            // 1. Khởi tạo ViewSheet
            ViewSheet sheet = ViewSheet.Create(doc, viewModel.SelectedTitleBlock.Id);
            sheet.Name = "SHOP DRAWINGS DẦM " + viewModel.BeamName;
            sheet.SheetNumber = "S-" + DateTime.Now.ToString("mmss");
            viewModel.CreatedSheet = sheet;

            // 2. Tạo mặt cắt dọc
            ViewSection longView = CreateLongitudinalSection(doc, viewModel.SelectedBeams, viewModel.SelectedSectionViewType.Id, scaleValue);
            if (longView != null)
            {
                // Áp dụng View Template nếu có chọn
                if (viewModel.SelectedLongitudinalViewTemplate != null)
                {
                    longView.ViewTemplateId = viewModel.SelectedLongitudinalViewTemplate.Id;
                }
                longView.Name = "Mặt Cắt Dọc dầm - " + viewModel.BeamName + " - " + DateTime.Now.ToString("mmss");
                viewModel.CreatedLongitudinalView = longView;

                // Đặt lên Sheet tại nửa dưới
                XYZ position = new XYZ(0.7, 0.35, 0);
                Viewport vp = Viewport.Create(doc, sheet.Id, longView.Id, position);
                if (viewModel.SelectedLongitudinalViewportType != null)
                {
                    vp.ChangeTypeId(viewModel.SelectedLongitudinalViewportType.Id);
                }
            }

            // 3. Sắp xếp dầm dọc theo trục collinear và tạo 3 mặt cắt ngang (0.1, 0.5, 0.9) cho từng dầm riêng biệt
            viewModel.CreatedCrossSections.Clear();

            Line firstBeamLine = GetBeamLine(viewModel.SelectedBeams[0]);
            if (firstBeamLine != null)
            {
                XYZ p0 = firstBeamLine.GetEndPoint(0);
                XYZ mainDir = firstBeamLine.Direction.Normalize();

                // Sắp xếp các dầm theo thứ tự dọc theo trục chính
                var sortedBeams = viewModel.SelectedBeams
                    .OrderBy(b => {
                        Line l = GetBeamLine(b);
                        if (l == null) return 0.0;
                        return (l.GetEndPoint(0) - p0).DotProduct(mainDir);
                    })
                    .ToList();

                var crossConfig = new[]
                {
                    new { Name = "Gối Trái", Ratio = 0.1 },
                    new { Name = "Giữa Nhịp", Ratio = 0.5 },
                    new { Name = "Gối Phải", Ratio = 0.9 }
                };

                for (int beamIndex = 0; beamIndex < sortedBeams.Count; beamIndex++)
                {
                    var beam = sortedBeams[beamIndex];
                    Line line = GetBeamLine(beam);
                    if (line == null) continue;

                    XYZ pStart = line.GetEndPoint(0);
                    XYZ pEnd = line.GetEndPoint(1);
                    XYZ beamDir = line.Direction.Normalize();
                    double segmentLength = pStart.DistanceTo(pEnd);

                    double beamWidth = 1.0;
                    double beamHeight = 2.0;
                    var typeId = beam.GetTypeId();
                    if (typeId != ElementId.InvalidElementId)
                    {
                        var typeElem = doc.GetElement(typeId);
                        if (typeElem != null)
                        {
                            var bParam = typeElem.LookupParameter("b") ?? typeElem.LookupParameter("Width") ?? typeElem.LookupParameter("B");
                            var hParam = typeElem.LookupParameter("h") ?? typeElem.LookupParameter("Height") ?? typeElem.LookupParameter("H");
                            if (bParam != null && bParam.HasValue) beamWidth = bParam.AsDouble();
                            if (hParam != null && hParam.HasValue) beamHeight = hParam.AsDouble();
                        }
                    }

                    for (int i = 0; i < crossConfig.Length; i++)
                    {
                        var config = crossConfig[i];
                        XYZ crossPoint = pStart + beamDir * (segmentLength * config.Ratio);

                        ViewSection crossView = CreateCrossSectionAtPoint(doc, crossPoint, beamDir, viewModel.SelectedDetailViewType.Id, scaleValue, beamWidth, beamHeight);
                        if (crossView != null)
                        {
                            // Áp dụng View Template nếu có chọn
                            if (viewModel.SelectedSectionViewTemplate != null)
                            {
                                crossView.ViewTemplateId = viewModel.SelectedSectionViewTemplate.Id;
                            }
                            crossView.Name = $"Mặt Cắt Ngang - {beam.Name} - {config.Name} - {DateTime.Now.ToString("mmss")} - {beamIndex}_{i}";
                            viewModel.CreatedCrossSections.Add(crossView);

                            // Sắp xếp đều các mặt cắt ngang thành lưới trên Sheet
                            // i (0 -> 2) là cột, beamIndex là dòng
                            double xPos = 0.25 + i * 0.25;
                            double yPos = 0.85 - beamIndex * 0.12;
                            XYZ position = new XYZ(xPos, yPos, 0);
                            Viewport vp = Viewport.Create(doc, sheet.Id, crossView.Id, position);
                            if (viewModel.SelectedCrossSectionViewportType != null)
                            {
                                vp.ChangeTypeId(viewModel.SelectedCrossSectionViewportType.Id);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Tạo Tag và Dimension tự động cho dầm và thép trên bản vẽ
        /// </summary>
        public static void AnnotateBeamDrawings(MainViewModel viewModel)
        {
            Document doc = viewModel.Doc;
            if (viewModel.CreatedSheet == null)
            {
                throw new Exception("Chưa khởi tạo Sheet bản vẽ!");
            }

            // 1. Lấy tất cả thép Rebar được host bởi các dầm đã chọn
            var hostIds = new HashSet<ElementId>(viewModel.SelectedBeams.Select(b => b.Id));

            var rebars = new FilteredElementCollector(doc)
                .OfClass(typeof(Rebar))
                .Cast<Rebar>()
                .Where(r => hostIds.Contains(r.GetHostId()))
                .ToList();

            int totalSuccess = 0;
            List<string> errors = new List<string>();

            // 2. Chèn Rebar Tags trên Mặt cắt dọc dầm
            if (viewModel.CreatedLongitudinalView != null)
            {
                totalSuccess += CreateRebarTags(doc, viewModel.CreatedLongitudinalView, rebars, viewModel, errors);
            }

            // 3. Chèn Rebar Tags trên các Mặt cắt ngang
            foreach (var crossView in viewModel.CreatedCrossSections)
            {
                totalSuccess += CreateRebarTags(doc, crossView, rebars, viewModel, errors);
            }

            if (totalSuccess == 0 && errors.Count > 0)
            {
                Autodesk.Revit.UI.TaskDialog.Show("Lỗi Tạo Tag", "Không thể tạo tag nào cho thép. Lỗi chi tiết:\n" + string.Join("\n", errors.Distinct().Take(5)));
            }

            // 4. Tạo Dimension liên tục trên Mặt cắt dọc dầm
            if (viewModel.CreatedLongitudinalView != null && viewModel.SelectedDimType != null)
            {
                CreateBeamDimensions(doc, viewModel.CreatedLongitudinalView, viewModel.SelectedBeams, viewModel.SelectedDimType);
            }
        }

        /// <summary>
        /// Lấy Line định vị của Dầm
        /// </summary>
        private static Line GetBeamLine(Element beam)
        {
            LocationCurve locCurve = beam.Location as LocationCurve;
            if (locCurve == null) return null;
            return locCurve.Curve as Line;
        }

        /// <summary>
        /// Tạo mặt cắt dọc chạy xuyên qua cả nhóm dầm thẳng hàng
        /// </summary>
        private static ViewSection CreateLongitudinalSection(Document doc, List<Element> beams, ElementId viewFamilyTypeId, double scaleValue)
        {
            if (beams == null || beams.Count == 0) return null;

            Line firstLine = GetBeamLine(beams[0]);
            if (firstLine == null) return null;

            XYZ p0 = firstLine.GetEndPoint(0);
            XYZ dir = firstLine.Direction.Normalize();

            // Tính điểm đầu và cuối cực trị bằng cách chiếu lên trục hướng chuẩn
            double minT = double.MaxValue;
            double maxT = double.MinValue;

            foreach (var beam in beams)
            {
                Line line = GetBeamLine(beam);
                if (line == null) continue;

                XYZ pStart = line.GetEndPoint(0);
                XYZ pEnd = line.GetEndPoint(1);

                double tStart = (pStart - p0).DotProduct(dir);
                double tEnd = (pEnd - p0).DotProduct(dir);

                minT = Math.Min(minT, Math.Min(tStart, tEnd));
                maxT = Math.Max(maxT, Math.Max(tStart, tEnd));
            }

            double length = maxT - minT;
            XYZ center = p0 + dir * ((minT + maxT) / 2.0);

            // Hệ trục toạ độ của mặt cắt dọc
            XYZ basisX = -dir;
            XYZ up = Math.Abs(dir.DotProduct(XYZ.BasisZ)) > 0.99 ? XYZ.BasisY : XYZ.BasisZ;
            XYZ basisZ = basisX.CrossProduct(up).Normalize();
            XYZ basisY = basisZ.CrossProduct(basisX).Normalize();

            Transform transform = Transform.Identity;
            transform.Origin = center;
            transform.BasisX = basisX;
            transform.BasisY = basisY;
            transform.BasisZ = basisZ;

            BoundingBoxXYZ box = new BoundingBoxXYZ();
            box.Transform = transform;

            // Lấy kích thước dầm (b, h) để làm căn cứ offset
            double beamWidth = 1.0;
            double beamHeight = 2.0;
            if (beams != null && beams.Count > 0)
            {
                var firstBeam = beams[0];
                var typeId = firstBeam.GetTypeId();
                if (typeId != ElementId.InvalidElementId)
                {
                    var typeElem = doc.GetElement(typeId);
                    if (typeElem != null)
                    {
                        var bParam = typeElem.LookupParameter("b") ?? typeElem.LookupParameter("Width") ?? typeElem.LookupParameter("B");
                        var hParam = typeElem.LookupParameter("h") ?? typeElem.LookupParameter("Height") ?? typeElem.LookupParameter("H");
                        if (bParam != null && bParam.HasValue) beamWidth = bParam.AsDouble();
                        if (hParam != null && hParam.HasValue) beamHeight = hParam.AsDouble();
                    }
                }
            }

            // Đệm 1.0 feet (~30cm) ở hai đầu biên
            double paddingX = 1.0;
            // Chiều cao (Y local) và chiều sâu nhìn (Z local) động theo kích thước dầm
            double minY = -beamHeight * 1.5;
            double maxY = beamHeight * 0.5;
            double minZ = -beamWidth * 1.5;
            double maxZ = beamWidth * 1.5;

            box.Min = new XYZ(-length / 2.0 - paddingX, minY, minZ);
            box.Max = new XYZ(length / 2.0 + paddingX, maxY, maxZ);

            ViewSection section = null;
            ViewFamilyType vft = doc.GetElement(viewFamilyTypeId) as ViewFamilyType;
            if (vft != null && vft.ViewFamily == ViewFamily.Detail)
            {
                section = ViewSection.CreateDetail(doc, viewFamilyTypeId, box);
            }
            else
            {
                section = ViewSection.CreateSection(doc, viewFamilyTypeId, box);
            }

            if (section != null)
            {
                section.Scale = (int)scaleValue;
            }
            return section;
        }

        /// <summary>
        /// Tạo mặt cắt ngang vuông góc tại điểm bất kỳ dọc theo hướng dầm
        /// </summary>
        private static ViewSection CreateCrossSectionAtPoint(Document doc, XYZ point, XYZ dir, ElementId viewFamilyTypeId, double scaleValue, double beamWidth, double beamHeight)
        {
            // Hệ trục toạ độ của mặt cắt ngang
            XYZ basisZ = -dir;
            XYZ up = Math.Abs(dir.DotProduct(XYZ.BasisZ)) > 0.99 ? XYZ.BasisY : XYZ.BasisZ;
            XYZ basisX = dir.CrossProduct(up).Normalize();
            XYZ basisY = basisZ.CrossProduct(basisX).Normalize();

            Transform transform = Transform.Identity;
            transform.Origin = point;
            transform.BasisX = basisX;
            transform.BasisY = basisY;
            transform.BasisZ = basisZ;

            BoundingBoxXYZ box = new BoundingBoxXYZ();
            box.Transform = transform;

            // Kích thước hộp cắt ngang dầm rộng khoảng 1.5 * beamWidth và cao 1.5 * beamHeight
            double minX = -beamWidth * 1.5;
            double maxX = beamWidth * 1.5;
            double minY = -beamHeight * 1.5;
            double maxY = beamHeight * 0.5;

            box.Min = new XYZ(minX, minY, -0.5);
            box.Max = new XYZ(maxX, maxY, 0.5);

            ViewSection section = null;
            ViewFamilyType vft = doc.GetElement(viewFamilyTypeId) as ViewFamilyType;
            if (vft != null && vft.ViewFamily == ViewFamily.Detail)
            {
                section = ViewSection.CreateDetail(doc, viewFamilyTypeId, box);
            }
            else
            {
                section = ViewSection.CreateSection(doc, viewFamilyTypeId, box);
            }

            if (section != null)
            {
                section.Scale = (int)scaleValue;
            }
            return section;
        }

        /// <summary>
        /// Tự động chèn Tag cho danh sách thép trong khung nhìn
        /// </summary>
        private static int CreateRebarTags(Document doc, ViewSection view, List<Rebar> rebars, MainViewModel viewModel, List<string> errors)
        {
            ElementId mainTagId = viewModel.SelectedLongitudinalMainRebarTag?.Id ?? ElementId.InvalidElementId;
            ElementId stirrupTagId = viewModel.SelectedLongitudinalStirrupTag?.Id ?? ElementId.InvalidElementId;

            bool isCrossSection = viewModel.CreatedCrossSections.Any(cv => cv.Id == view.Id);
            if (isCrossSection && viewModel.SelectedCrossSectionStirrupTag != null)
            {
                stirrupTagId = viewModel.SelectedCrossSectionStirrupTag.Id;
            }

            if (rebars == null || rebars.Count == 0) return 0;

            int successCount = 0;

            foreach (var rebar in rebars)
            {
                try
                {
                    BoundingBoxXYZ bbox = rebar.get_BoundingBox(view) ?? rebar.get_BoundingBox(null);
                    if (bbox == null) continue;

                    XYZ localCenter = (bbox.Min + bbox.Max) / 2.0;
                    XYZ center = bbox.Transform.OfPoint(localCenter);

                    string mark = "";
                    Parameter markParam = rebar.get_Parameter(BuiltInParameter.ALL_MODEL_MARK);
                    if (markParam != null && markParam.HasValue)
                    {
                        mark = markParam.AsString();
                    }

                    ElementId tagSymbolId = ElementId.InvalidElementId;
                    XYZ tagPos = center;

                    double paperOffset = 10; // 4mm trên bản vẽ giấy
                    double offsetFeet = (paperOffset * view.Scale) / 304.8; // Quy đổi ra Feet trong mô hình (200mm ở tỷ lệ 1:50)

                    if (mark.Contains("TOP"))
                    {
                        tagSymbolId = mainTagId;
                        tagPos = center + view.UpDirection * offsetFeet;
                    }
                    else if (mark.Contains("BOT"))
                    {
                        tagSymbolId = mainTagId;
                        tagPos = center - view.UpDirection * offsetFeet;
                    }
                    else if (mark.Contains("STIRRUP"))
                    {
                        tagSymbolId = mainTagId;
                        tagPos = center + view.UpDirection * 2*offsetFeet;
                    }
                    else 
                    {
                        tagSymbolId = mainTagId;
                        tagPos = center + view.UpDirection * offsetFeet;
                    }

                    if (isCrossSection && !string.Equals(mark, "Stirrup", StringComparison.OrdinalIgnoreCase) && viewModel.SelectedCrossSectionMainRebarTag != null)
                    {
                        MultiReferenceAnnotationOptions options = new MultiReferenceAnnotationOptions(viewModel.SelectedCrossSectionMainRebarTag);
                        options.TagHeadPosition = tagPos;
                        options.DimensionLineOrigin = center;
                        options.DimensionLineDirection = view.RightDirection;
                        options.DimensionPlaneNormal = view.ViewDirection;
                        options.SetElementsToDimension(new List<ElementId> { rebar.Id });

                        MultiReferenceAnnotation mra = MultiReferenceAnnotation.Create(doc, view.Id, options);
                        if (mra != null)
                        {
                            successCount++;
                        }
                    }
                    else
                    {
                        if (tagSymbolId == ElementId.InvalidElementId) continue;

                        Reference rebarRef = null;
                        IList<Subelement> subelements = rebar.GetSubelements();
                        if (subelements != null && subelements.Count > 0)
                        {
                            rebarRef = subelements[0].GetReference();
                        }
                        else
                        {
                            rebarRef = new Reference(rebar);
                        }

                        IndependentTag tag = IndependentTag.Create(
                            doc,
                            tagSymbolId,
                            view.Id,
                            rebarRef,
                            true,
                            TagOrientation.Horizontal,
                            tagPos
                        );

                        if (tag != null)
                        {
                            // Giữ liên kết leader gắn vào thanh thép (Attached), chỉ điều chỉnh vị trí đặt đầu tag
                            tag.TagHeadPosition = tagPos;
                            successCount++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    errors.Add(ex.Message);
                }
            }

            return successCount;
        }

        /// <summary>
        /// Tự động tạo Dimension chạy dọc cả nhóm dầm thẳng hàng
        /// </summary>
        private static void CreateBeamDimensions(Document doc, ViewSection view, List<Element> beams, DimensionType dimType)
        {
            if (dimType == null || beams == null || beams.Count == 0) return;

            Line firstLine = GetBeamLine(beams[0]);
            if (firstLine == null) return;

            XYZ dir = firstLine.Direction.Normalize();
            XYZ p0 = firstLine.GetEndPoint(0);

            // Vector vuông góc để dịch chuyển đường Dimension
            XYZ perp = dir.CrossProduct(XYZ.BasisZ).Normalize();

            ReferenceArray refArray = new ReferenceArray();

            // Lấy mặt bên xuất phát của dầm đầu tiên
            Face startFace = GetBeamEndFace(beams[0], dir, true);
            if (startFace != null && startFace.Reference != null)
            {
                refArray.Append(startFace.Reference);
            }

            // Lấy mặt bên kết thúc của từng dầm
            foreach (var beam in beams)
            {
                Face endFace = GetBeamEndFace(beam, dir, false);
                if (endFace != null && endFace.Reference != null)
                {
                    refArray.Append(endFace.Reference);
                }
            }

            if (refArray.Size >= 2)
            {
                // Vị trí đặt Dimension cách tim dầm 1.2 feet (~36cm)
                XYZ center = p0 + dir * (beams.Count * 2.0);
                XYZ dimLineStart = center + perp * 1.2;
                XYZ dimLineEnd = dimLineStart + dir * 5.0;

                Line dimLine = Line.CreateBound(dimLineStart, dimLineEnd);

                doc.Create.NewDimension(view, dimLine, refArray, dimType);
            }
        }

        /// <summary>
        /// Trích xuất mặt bên vuông góc của Dầm từ Hình học Revit (Fine Detail)
        /// </summary>
        private static Face GetBeamEndFace(Element beam, XYZ direction, bool getStart)
        {
            Options opt = new Options { ComputeReferences = true, DetailLevel = ViewDetailLevel.Fine };
            GeometryElement geomElem = beam.get_Geometry(opt);
            if (geomElem == null) return null;

            foreach (GeometryObject geomObj in geomElem)
            {
                if (geomObj is Solid solid)
                {
                    if (solid.Volume <= 0) continue;

                    foreach (Face face in solid.Faces)
                    {
                        // Kiểm tra Normal vuông góc với trục dầm
                        XYZ normal = face.ComputeNormal(new UV(0.5, 0.5));
                        double dot = normal.DotProduct(direction);

                        // Tìm mặt xuất phát (ngược hướng dầm)
                        if (getStart && dot < -0.99)
                        {
                            if (face.Reference != null) return face;
                        }
                        // Tìm mặt kết thúc (cùng hướng dầm)
                        else if (!getStart && dot > 0.99)
                        {
                            if (face.Reference != null) return face;
                        }
                    }
                }
            }
            return null;
        }
    }
}
