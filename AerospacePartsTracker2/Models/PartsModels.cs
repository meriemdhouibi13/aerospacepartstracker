namespace AerospacePartsTracker2.Models;

// ── Module 1: Geometry ──────────────────────────────────────
public class Point3D
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }
}

public class GeoInput
{
    public Point3D P1 { get; set; } = new();
    public Point3D P2 { get; set; } = new();
    public Point3D P3 { get; set; } = new();
}

public class GeometryResult
{
    public string PlaneEquation { get; set; } = "";
    public string UnitNormal { get; set; } = "";
    public double DistanceFromOrigin { get; set; }
    public bool IsValid { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;

    // Raw coefficients for PDF export
    public double A { get; set; }
    public double B { get; set; }
    public double C { get; set; }
    public double D { get; set; }
}

// ── Module 2: Tolerance ─────────────────────────────────────
public class TolInput
{
    public string PartId { get; set; } = "";
    public double NominalLength { get; set; }
    public double StdDev { get; set; }
    public int SampleSize { get; set; }
    public double SampleMean { get; set; }
    public double ToleranceLimit { get; set; } = 0.5;
}

public class ToleranceResult
{
    public string PartId { get; set; } = "";
    public double CILower { get; set; }
    public double CIUpper { get; set; }
    public double ProbWithinTolerance { get; set; }
    public double Cp { get; set; }
    public double Cpk { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;
}

public class NormalDistPoint
{
    public decimal X { get; set; }
    public double Y { get; set; }
}

// ── Module 3: BOM ───────────────────────────────────────────
public class BomInput
{
    public int QtyA { get; set; }
    public int QtyB { get; set; }
    public int QtyC { get; set; }
}

public class BomResult
{
    public int TotalScrews { get; set; }
    public int SmallPacks { get; set; }
    public int MediumPacks { get; set; }
    public int LargePacks { get; set; }
    public int TotalCost { get; set; }
    public int Leftover { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;
}

public class PackBarPoint
{
    public PackBarPoint(string label, int count) { Label = label; Count = count; }
    public string Label { get; set; }
    public int Count { get; set; }
}