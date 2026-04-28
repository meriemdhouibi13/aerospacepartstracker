using MathNet.Numerics.Distributions;
using AerospacePartsTracker2.Models;

namespace AerospacePartsTracker2.Services;

/// <summary>
/// All engineering mathematics for the PartsManufacturing page.
/// Register as Scoped in Program.cs:  builder.Services.AddScoped&lt;PartsCalculationService&gt;();
/// </summary>
public class PartsCalculationService
{
    // ════════════════════════════════════════════════════════
    //  MODULE 1 — PLANE GEOMETRY (Linear Algebra)
    // ════════════════════════════════════════════════════════

    /// <summary>
    /// Given three 3-D points, computes:
    ///   • Plane equation  ax + by + cz + d = 0
    ///   • Unit normal vector  n̂
    ///   • Perpendicular distance from the origin to the plane
    /// </summary>
    public GeometryResult ComputeGeometry(GeoInput input)
    {
        var (p1, p2, p3) = (input.P1, input.P2, input.P3);

        // Edge vectors
        double[] v1 = { p2.X - p1.X, p2.Y - p1.Y, p2.Z - p1.Z };
        double[] v2 = { p3.X - p1.X, p3.Y - p1.Y, p3.Z - p1.Z };

        // Normal via cross product  n = v1 × v2
        double a = v1[1] * v2[2] - v1[2] * v2[1];
        double b = v1[2] * v2[0] - v1[0] * v2[2];
        double c = v1[0] * v2[1] - v1[1] * v2[0];

        double magnitude = Math.Sqrt(a * a + b * b + c * c);

        if (magnitude < 1e-10)
            throw new InvalidOperationException("The three points are collinear — they do not define a unique plane.");

        // d from point-normal form:  n · P1 + d = 0
        double d = -(a * p1.X + b * p1.Y + c * p1.Z);

        // Unit normal
        double nx = a / magnitude;
        double ny = b / magnitude;
        double nz = c / magnitude;

        // Distance from origin = |d| / |n|
        double distFromOrigin = Math.Abs(d) / magnitude;

        // Build human-readable plane equation
        string planeEq = FormatPlaneEquation(a, b, c, d);

        return new GeometryResult
        {
            A = a,
            B = b,
            C = c,
            D = d,
            PlaneEquation = planeEq,
            UnitNormal = $"({nx:F4}, {ny:F4}, {nz:F4})",
            DistanceFromOrigin = distFromOrigin,
            IsValid = distFromOrigin > 1e-6,   // plane not through origin = valid orientation
            Timestamp = DateTime.Now
        };
    }

    private static string FormatPlaneEquation(double a, double b, double c, double d)
    {
        // Produce a tidy string like: 1.2340x - 0.5000y + 2.1000z + -3.4500 = 0
        string Sign(double v, bool first) =>
            first ? $"{v:F4}" : (v >= 0 ? $"+ {v:F4}" : $"- {Math.Abs(v):F4}");

        return $"{Sign(a, true)}x {Sign(b, false)}y {Sign(c, false)}z {Sign(d, false)} = 0";
    }

    // ════════════════════════════════════════════════════════
    //  MODULE 2 — TOLERANCE STACK-UP (Statistics)
    // ════════════════════════════════════════════════════════

    /// <summary>
    /// Computes:
    ///   • 95 % confidence interval for the true mean
    ///   • P(part within ±tolerance of nominal)
    ///   • Cp and Cpk process capability indices
    /// Uses MathNet.Numerics for the normal distribution.
    /// </summary>
    public ToleranceResult ComputeTolerance(TolInput input)
    {
        if (input.StdDev <= 0)
            throw new InvalidOperationException("Standard deviation must be greater than zero.");
        if (input.SampleSize < 2)
            throw new InvalidOperationException("Sample size must be at least 2.");

        double mean = input.SampleMean;
        double sigma = input.StdDev;
        int n = input.SampleSize;
        double nom = input.NominalLength;
        double tol = input.ToleranceLimit;

        // 95% CI using z=1.96 (large-sample approximation)
        // For small samples, a t-distribution would be more accurate,
        // but aerospace QC typically uses large n.
        double zCrit = 1.95996;   // z_{0.025}
        double se = sigma / Math.Sqrt(n);
        double ciLo = mean - zCrit * se;
        double ciHi = mean + zCrit * se;

        // P(|X - nominal| <= tol) via standard normal CDF
        var dist = new Normal(mean, sigma);
        double prob = dist.CumulativeDistribution(nom + tol)
                    - dist.CumulativeDistribution(nom - tol);

        // Process limits: USL = nominal + tol, LSL = nominal - tol
        double usl = nom + tol;
        double lsl = nom - tol;

        // Cp = (USL - LSL) / (6σ)
        double cp = (usl - lsl) / (6.0 * sigma);

        // Cpk = min( (USL - μ)/(3σ), (μ - LSL)/(3σ) )
        double cpk = Math.Min((usl - mean) / (3.0 * sigma),
                              (mean - lsl) / (3.0 * sigma));

        return new ToleranceResult
        {
            PartId = input.PartId,
            CILower = ciLo,
            CIUpper = ciHi,
            ProbWithinTolerance = prob,
            Cp = cp,
            Cpk = cpk,
            Timestamp = DateTime.Now
        };
    }

    /// <summary>
    /// Generates 200 (x, pdf(x)) points for the ApexCharts area series.
    /// Covers mean ± 4σ, with vertical markers at ±tolerance boundaries.
    /// </summary>
    public List<NormalDistPoint> BuildNormalCurve(
        double mean, double sigma, double nominal, double tol)
    {
        var dist = new Normal(mean, sigma);
        var points = new List<NormalDistPoint>();

        double xMin = mean - 4 * sigma;
        double xMax = mean + 4 * sigma;
        int steps = 200;

        for (int i = 0; i <= steps; i++)
        {
            double x = xMin + (xMax - xMin) * i / steps;
            points.Add(new NormalDistPoint
            {
                X = (decimal)Math.Round(x, 6),
                Y = dist.Density(x)
            });
        }
        return points;
    }

    // ════════════════════════════════════════════════════════
    //  MODULE 3 — BOM OPTIMIZER (Integer Optimization)
    // ════════════════════════════════════════════════════════

    // Pack definitions
    private record Pack(int Qty, int Cost);
    private static readonly Pack Small = new(50, 20);
    private static readonly Pack Medium = new(120, 45);
    private static readonly Pack Large = new(300, 100);

    /// <summary>
    /// Brute-force integer optimization over a bounded search space.
    /// Finds the minimum-cost combination of S/M/L packs that covers
    /// the total screw demand for the requested assembly quantities.
    /// </summary>
    public BomResult ComputeBom(BomInput input)
    {
        const int screwsA = 12, screwsB = 23, screwsC = 8;

        int totalNeeded = input.QtyA * screwsA
                        + input.QtyB * screwsB
                        + input.QtyC * screwsC;

        if (totalNeeded <= 0)
            throw new InvalidOperationException("Enter at least one assembly quantity > 0.");

        // Upper bounds: no need to buy more than ceil(totalNeeded / packSize) of each
        int maxL = totalNeeded / Large.Qty + 1;
        int maxM = totalNeeded / Medium.Qty + 1;
        int maxS = totalNeeded / Small.Qty + 1;

        int bestCost = int.MaxValue;
        int bestL = 0, bestM = 0, bestS = 0;

        for (int l = 0; l <= maxL; l++)
        {
            for (int m = 0; m <= maxM; m++)
            {
                // After choosing l and m, compute minimum small packs needed
                int covered = l * Large.Qty + m * Medium.Qty;
                if (covered >= totalNeeded)
                {
                    // No smalls needed — evaluate directly
                    int cost = l * Large.Cost + m * Medium.Cost;
                    if (cost < bestCost)
                    {
                        bestCost = cost; bestL = l; bestM = m; bestS = 0;
                    }
                    continue;
                }

                int remaining = totalNeeded - covered;
                int s = (int)Math.Ceiling((double)remaining / Small.Qty);
                int totalCost = l * Large.Cost + m * Medium.Cost + s * Small.Cost;

                if (totalCost < bestCost)
                {
                    bestCost = totalCost; bestL = l; bestM = m; bestS = s;
                }
            }
        }

        int totalProvided = bestL * Large.Qty + bestM * Medium.Qty + bestS * Small.Qty;

        return new BomResult
        {
            TotalScrews = totalNeeded,
            LargePacks = bestL,
            MediumPacks = bestM,
            SmallPacks = bestS,
            TotalCost = bestCost,
            Leftover = totalProvided - totalNeeded,
            Timestamp = DateTime.Now
        };
    }
}