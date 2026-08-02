// Dart parity source: flutter/packages/flutter/lib/src/gestures/lsq_solver.dart

namespace Plumix.Gestures;

internal sealed class PolynomialFit
{
    public PolynomialFit(int degree)
    {
        Coefficients = new double[degree + 1];
    }

    public double[] Coefficients { get; }

    public double Confidence { get; set; }
}

internal sealed class LeastSquaresSolver
{
    private const double PrecisionErrorTolerance = 1e-10;
    private readonly IReadOnlyList<double> _weights;
    private readonly IReadOnlyList<double> _x;
    private readonly IReadOnlyList<double> _y;

    public LeastSquaresSolver(
        IReadOnlyList<double> x,
        IReadOnlyList<double> y,
        IReadOnlyList<double> weights)
    {
        if (x.Count != y.Count || y.Count != weights.Count)
        {
            throw new ArgumentException("Least-squares inputs must have equal lengths.");
        }

        _x = x;
        _y = y;
        _weights = weights;
    }

    public PolynomialFit? Solve(int degree)
    {
        if (degree > _x.Count)
        {
            return null;
        }

        var result = new PolynomialFit(degree);
        int sampleCount = _x.Count;
        int coefficientCount = degree + 1;
        var a = new Matrix(coefficientCount, sampleCount);
        for (int sample = 0; sample < sampleCount; sample++)
        {
            a.Set(0, sample, _weights[sample]);
            for (int coefficient = 1; coefficient < coefficientCount; coefficient++)
            {
                a.Set(coefficient, sample, a.Get(coefficient - 1, sample) * _x[sample]);
            }
        }

        var q = new Matrix(coefficientCount, sampleCount);
        var r = new Matrix(coefficientCount, coefficientCount);
        for (int column = 0; column < coefficientCount; column++)
        {
            for (int sample = 0; sample < sampleCount; sample++)
            {
                q.Set(column, sample, a.Get(column, sample));
            }

            for (int previous = 0; previous < column; previous++)
            {
                double dot = q.GetRow(column).Dot(q.GetRow(previous));
                for (int sample = 0; sample < sampleCount; sample++)
                {
                    q.Set(column, sample, q.Get(column, sample) - (dot * q.Get(previous, sample)));
                }
            }

            double norm = q.GetRow(column).Norm();
            if (norm < PrecisionErrorTolerance)
            {
                return null;
            }

            double inverseNorm = 1.0 / norm;
            for (int sample = 0; sample < sampleCount; sample++)
            {
                q.Set(column, sample, q.Get(column, sample) * inverseNorm);
            }

            for (int coefficient = 0; coefficient < coefficientCount; coefficient++)
            {
                r.Set(
                    column,
                    coefficient,
                    coefficient < column ? 0.0 : q.GetRow(column).Dot(a.GetRow(coefficient)));
            }
        }

        var weightedY = new VectorStorage(sampleCount);
        for (int sample = 0; sample < sampleCount; sample++)
        {
            weightedY[sample] = _y[sample] * _weights[sample];
        }

        for (int coefficient = coefficientCount - 1; coefficient >= 0; coefficient--)
        {
            result.Coefficients[coefficient] = q.GetRow(coefficient).Dot(weightedY);
            for (int solved = coefficientCount - 1; solved > coefficient; solved--)
            {
                result.Coefficients[coefficient] -= r.Get(coefficient, solved) * result.Coefficients[solved];
            }

            result.Coefficients[coefficient] /= r.Get(coefficient, coefficient);
        }

        double yMean = 0.0;
        for (int sample = 0; sample < sampleCount; sample++)
        {
            yMean += _y[sample];
        }

        yMean /= sampleCount;
        double sumSquaredError = 0.0;
        double sumSquaredTotal = 0.0;
        for (int sample = 0; sample < sampleCount; sample++)
        {
            double term = 1.0;
            double error = _y[sample] - result.Coefficients[0];
            for (int coefficient = 1; coefficient < coefficientCount; coefficient++)
            {
                term *= _x[sample];
                error -= term * result.Coefficients[coefficient];
            }

            double squaredWeight = _weights[sample] * _weights[sample];
            sumSquaredError += squaredWeight * error * error;
            double variance = _y[sample] - yMean;
            sumSquaredTotal += squaredWeight * variance * variance;
        }

        result.Confidence = sumSquaredTotal <= PrecisionErrorTolerance
            ? 1.0
            : 1.0 - (sumSquaredError / sumSquaredTotal);
        return result;
    }

    private sealed class Matrix
    {
        private readonly int _columns;
        private readonly double[] _elements;

        public Matrix(int rows, int columns)
        {
            _columns = columns;
            _elements = new double[rows * columns];
        }

        public double Get(int row, int column) => _elements[(row * _columns) + column];

        public VectorStorage GetRow(int row) => new(_elements, row * _columns, _columns);

        public void Set(int row, int column, double value)
        {
            _elements[(row * _columns) + column] = value;
        }
    }

    private sealed class VectorStorage
    {
        private readonly double[] _elements;
        private readonly int _length;
        private readonly int _offset;

        public VectorStorage(int length) : this(new double[length], 0, length)
        {
        }

        public VectorStorage(double[] elements, int offset, int length)
        {
            _elements = elements;
            _offset = offset;
            _length = length;
        }

        public double this[int index]
        {
            get => _elements[index + _offset];
            set => _elements[index + _offset] = value;
        }

        public double Dot(VectorStorage other)
        {
            double result = 0.0;
            for (int index = 0; index < _length; index++)
            {
                result += this[index] * other[index];
            }

            return result;
        }

        public double Norm() => Math.Sqrt(Dot(this));
    }
}
