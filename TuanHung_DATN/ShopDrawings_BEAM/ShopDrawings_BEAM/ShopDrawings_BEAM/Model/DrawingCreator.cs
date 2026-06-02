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
                Viewport.Create(doc, sheet.Id, longView.Id, position);
            }

            // 3. Tạo đúng 3 mặt cắt ngang dọc theo đường dầm liên tục: Gối Trái (15%), Giữa Nhịp (50%), Gối Phải (85%)
            viewModel.CreatedCrossSections.Clear();
            
            Line firstBeamLine = GetBeamLine(viewModel.SelectedBeams[0]);
            if (firstBeamLine != null)
            {
                XYZ p0 = firstBeamLine.GetEndPoint(0);
                XYZ dir = firstBeamLine.Direction.Normalize();

                double minT = double.MaxValue;
                double maxT = double.MinValue;

                foreach (var beam in viewModel.SelectedBeams)
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

                double totalLength = maxT - minT;
                XYZ startPoint = p0 + dir * minT;

                var crossConfig = new[]
                {
                    new { Name = "Gối Trái", Ratio = 0.15 },
                    new { Name = "Giữa Nhịp", Ratio = 0.50 },
                    new { Name = "Gối Phải", Ratio = 0.85 }
                };

                for (int i = 0; i < crossConfig.Length; i++)
                {
                    var config = crossConfig[i];
                    XYZ crossPoint = startPoint + dir * (totalLength * config.Ratio);

                    ViewSection crossView = CreateCrossSectionAtPoint(doc, crossPoint, dir, viewModel.SelectedSectionViewType.Id, scaleValue);
                    if (crossView != null)
                    {
                        // Áp dụng View Template nếu có chọn
                        if (viewModel.SelectedSectionViewTemplate != null)
                        {
                            crossView.ViewTemplateId = viewModel.SelectedSectionViewTemplate.Id;
                        }
                        crossView.Name = $"Mặt Cắt Ngang - {config.Name} - " + DateTime.Now.ToString("mmss");
                        viewModel.CreatedCrossSections.Add(crossView);

                        // Sắp xếp đều 3 mặt cắt ngang ở nửa trên của Sheet
                        double xPos = 0.35 + i * 0.35;
                        XYZ position = new XYZ(xPos, 0.75, 0);
                        Viewport.Create(doc, sheet.Id, crossView.Id, position);
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

            // 2. Chèn Rebar Tags trên Mặt cắt dọc dầm
            if (viewModel.CreatedLongitudinalView != null && viewModel.SelectedLongitudinalMainRebarTag != null)
            {
                CreateRebarTags(doc, viewModel.CreatedLongitudinalView, rebars, viewModel.SelectedLongitudinalMainRebarTag.Id);
            }

            // 3. Chèn Rebar Tags trên các Mặt cắt ngang
            if (viewModel.SelectedLongitudinalStirrupTag != null)
            {
                foreach (var crossView in viewModel.CreatedCrossSections)
                {
                    CreateRebarTags(doc, crossView, rebars, viewModel.SelectedLongitudinalStirrupTag.Id);
                }
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
            XYZ basisX = dir;
            XYZ basisY = XYZ.BasisZ;
            XYZ basisZ = basisX.CrossProduct(basisY).Normalize();

            Transform transform = Transform.Identity;
            transform.Origin = center;
            transform.BasisX = basisX;
            transform.BasisY = basisY;
            transform.BasisZ = basisZ;

            BoundingBoxXYZ box = new BoundingBoxXYZ();
            box.Transform = transform;

            // Đệm 1.0 feet (~30cm) ở hai đầu biên
            double paddingX = 1.0;
            box.Min = new XYZ(-length / 2.0 - paddingX, -3.0, -1.0);
            box.Max = new XYZ(length / 2.0 + paddingX, 3.0, 1.0);

            ViewSection section = ViewSection.CreateSection(doc, viewFamilyTypeId, box);
            if (section != null)
            {
                section.Scale = (int)scaleValue;
            }
            return section;
        }

        /// <summary>
        /// Tạo mặt cắt ngang vuông góc tại điểm bất kỳ dọc theo hướng dầm
        /// </summary>
        private static ViewSection CreateCrossSectionAtPoint(Document doc, XYZ point, XYZ dir, ElementId viewFamilyTypeId, double scaleValue)
        {
            // Hệ trục toạ độ của mặt cắt ngang
            XYZ basisX = dir.CrossProduct(XYZ.BasisZ).Normalize();
            XYZ basisY = XYZ.BasisZ;
            XYZ basisZ = dir;

            Transform transform = Transform.Identity;
            transform.Origin = point;
            transform.BasisX = basisX;
            transform.BasisY = basisY;
            transform.BasisZ = basisZ;

            BoundingBoxXYZ box = new BoundingBoxXYZ();
            box.Transform = transform;

            // Kích thước hộp cắt ngang dầm rộng khoảng 1.5ft (~45cm) và cao 2.5ft (~75cm)
            box.Min = new XYZ(-1.5, -2.5, -0.5);
            box.Max = new XYZ(1.5, 2.5, 0.5);

            ViewSection section = ViewSection.CreateSection(doc, viewFamilyTypeId, box);
            if (section != null)
            {
                section.Scale = (int)scaleValue;
            }
            return section;
        }

        /// <summary>
        /// Tự động chèn Tag cho danh sách thép trong khung nhìn
        /// </summary>
        private static void CreateRebarTags(Document doc, ViewSection view, List<Rebar> rebars, ElementId tagSymbolId)
        {
            if (tagSymbolId == ElementId.InvalidElementId || rebars == null || rebars.Count == 0) return;

            foreach (var rebar in rebars)
            {
                try
                {
                    // Lấy BoundingBox của Rebar trong View để xác định vị trí chèn Tag
                    BoundingBoxXYZ bbox = rebar.get_BoundingBox(view);
                    if (bbox == null) continue;

                    XYZ center = (bbox.Min + bbox.Max) / 2.0;
                    XYZ tagPos = center + new XYZ(0, 0.3, 0); // Đặt lệch lên trên 0.3 feet (~9cm)

                    Reference rebarRef = new Reference(rebar);

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
                        tag.LeaderEndCondition = LeaderEndCondition.Free;
                        //tag.LeaderEnd = center;
                        tag.TagHeadPosition = tagPos;
                    }
                }
                catch
                {
                    // Bỏ qua lỗi với các thanh thép riêng lẻ không thể Tag
                }
            }
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
