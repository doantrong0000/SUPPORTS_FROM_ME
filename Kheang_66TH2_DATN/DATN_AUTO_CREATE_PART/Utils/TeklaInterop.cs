using System;
using System.Collections.Generic;
using DATN_AUTO_CREATE_PART.Models;
using Tekla.Structures.Model;
using Tekla.Structures.Geometry3d;
using Point = Tekla.Structures.Geometry3d.Point;

namespace DATN_AUTO_CREATE_PART.Utils
{
    public static class TeklaInterop
    {
        public static void GenerateBeams(IEnumerable<BeamInfoCollection> beamCollections, XyzData originPoint, Point teklaOrigin)
        {
            Model model = new Model();
            if (!model.GetConnectionStatus()) return;

            foreach (var collection in beamCollections)
            {
                // Create a parametric profile string e.g. "400*200"
                string profileString = $"{collection.Height}*{collection.Width}";
                
                foreach (var beamInfo in collection.BeamInfos)
                {
                    // Map points relative to the origin
                    double dx1 = beamInfo.StartPoint.X - originPoint.X;
                    double dy1 = beamInfo.StartPoint.Y - originPoint.Y;
                    
                    double dx2 = beamInfo.EndPoint.X - originPoint.X;
                    double dy2 = beamInfo.EndPoint.Y - originPoint.Y;

                    Point p1 = new Point(teklaOrigin.X + dx1, teklaOrigin.Y + dy1, teklaOrigin.Z);
                    Point p2 = new Point(teklaOrigin.X + dx2, teklaOrigin.Y + dy2, teklaOrigin.Z);

                    Beam teklaBeam = new Beam(p1, p2)
                    {
                        Profile = { ProfileString = profileString },
                        Material = { MaterialString = "C25/30" },
                        Class = "3",
                        Name = collection.Text
                    };

                    teklaBeam.Insert();
                }
            }
            model.CommitChanges();
        }

        public static void GenerateColumns(IEnumerable<ColumnInfoCollection> columnCollections, XyzData originPoint, Point teklaOrigin)
        {
            Model model = new Model();
            if (!model.GetConnectionStatus()) return;

            foreach (var collection in columnCollections)
            {
                string profileString = $"{collection.Height}*{collection.Width}";

                foreach (var colInfo in collection.ColumnInfos)
                {
                    double dx = colInfo.Center.X - originPoint.X;
                    double dy = colInfo.Center.Y - originPoint.Y;

                    Point p1 = new Point(teklaOrigin.X + dx, teklaOrigin.Y + dy, teklaOrigin.Z);
                    Point p2 = new Point(teklaOrigin.X + dx, teklaOrigin.Y + dy, teklaOrigin.Z + 3000); // Default 3000 height

                    Beam teklaColumn = new Beam(p1, p2)
                    {
                        Profile = { ProfileString = profileString },
                        Material = { MaterialString = "C25/30" },
                        Class = "4",
                        Name = collection.Text
                    };
                    
                    // Center and Rotate
                    teklaColumn.Position.Plane = Position.PlaneEnum.MIDDLE;
                    teklaColumn.Position.Depth = Position.DepthEnum.MIDDLE;
                    teklaColumn.Position.Rotation = Position.RotationEnum.FRONT;
                    teklaColumn.Position.RotationOffset = colInfo.Rotation * 180 / Math.PI;

                    teklaColumn.Insert();
                }
            }
            model.CommitChanges();
        }

        public static void GenerateFloors(IEnumerable<FloorInfoCollection> floorCollections, XyzData originPoint, Point teklaOrigin)
        {
            Model model = new Model();
            if (!model.GetConnectionStatus()) return;

            foreach (var collection in floorCollections)
            {
                foreach (var pts in collection.FloorPoints)
                {
                    ContourPlate cp = new ContourPlate();
                    cp.Profile.ProfileString = collection.Thickness.ToString(); // Use input thickness
                    cp.Material.MaterialString = "C25/30";
                    cp.Class = "1";

                    foreach (var pt in pts)
                    {
                        double dx = pt.X - originPoint.X;
                        double dy = pt.Y - originPoint.Y;

                        Point cpPoint = new Point(teklaOrigin.X + dx, teklaOrigin.Y + dy, teklaOrigin.Z);
                        cp.AddContourPoint(new ContourPoint(cpPoint, new Chamfer()));
                    }

                    cp.Insert();
                }
            }
            model.CommitChanges();
        }

        public static void GenerateStandardGrid(Point teklaOrigin, GridInfo gridSettings)
        {
            Model model = new Model();
            if (!model.GetConnectionStatus()) return;

            // Delete existing grids first
            var gridEnum = model.GetModelObjectSelector().GetAllObjectsWithType(ModelObject.ModelObjectEnum.GRID);
            while (gridEnum.MoveNext())
            {
                if (gridEnum.Current != null)
                {
                    gridEnum.Current.Delete();
                }
            }

            Grid grid = new Grid
            {
                CoordinateX = ShiftFirstCoordinate(gridSettings.CoordinateX, teklaOrigin.X),
                CoordinateY = ShiftFirstCoordinate(gridSettings.CoordinateY, teklaOrigin.Y),
                CoordinateZ = ShiftFirstCoordinate(gridSettings.CoordinateZ, teklaOrigin.Z),
                LabelX = gridSettings.LabelX,
                LabelY = gridSettings.LabelY,
                LabelZ = gridSettings.LabelZ
            };

            grid.Insert();
            model.CommitChanges();
        }

        private static string ShiftFirstCoordinate(string coordStr, double shiftAmount)
        {
            if (string.IsNullOrWhiteSpace(coordStr) || Math.Abs(shiftAmount) < 0.01) return coordStr;

            var parts = coordStr.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 0 && double.TryParse(parts[0], out double firstVal))
            {
                parts[0] = (firstVal + shiftAmount).ToString("F1");
            }
            return string.Join(" ", parts);
        }
    }
}
