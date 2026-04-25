using Avalonia;

// Dart parity source (reference): flutter/packages/flutter/lib/src/painting/box_fit.dart (approximate)

namespace Plumix.Rendering;

public enum BoxFit
{
    Fill,
    Contain,
    Cover,
    FitWidth,
    FitHeight,
    None,
    ScaleDown
}

public readonly record struct FittedSizes(Size Source, Size Destination);

public static class BoxFitUtils
{
    public static FittedSizes ApplyBoxFit(BoxFit fit, Size inputSize, Size outputSize)
    {
        if (inputSize.Width <= 0.0 ||
            inputSize.Height <= 0.0 ||
            outputSize.Width <= 0.0 ||
            outputSize.Height <= 0.0)
        {
            return new FittedSizes(new Size(), new Size());
        }

        Size sourceSize;
        Size destinationSize;

        switch (fit)
        {
            case BoxFit.Fill:
                sourceSize = inputSize;
                destinationSize = outputSize;
                break;

            case BoxFit.Contain:
                sourceSize = inputSize;
                if (outputSize.Width / outputSize.Height > sourceSize.Width / sourceSize.Height)
                {
                    destinationSize = new Size(
                        sourceSize.Width * outputSize.Height / sourceSize.Height,
                        outputSize.Height);
                }
                else
                {
                    destinationSize = new Size(
                        outputSize.Width,
                        sourceSize.Height * outputSize.Width / sourceSize.Width);
                }

                break;

            case BoxFit.Cover:
                if (outputSize.Width / outputSize.Height > inputSize.Width / inputSize.Height)
                {
                    sourceSize = new Size(inputSize.Width, inputSize.Width * outputSize.Height / outputSize.Width);
                }
                else
                {
                    sourceSize = new Size(inputSize.Height * outputSize.Width / outputSize.Height, inputSize.Height);
                }

                destinationSize = outputSize;
                break;

            case BoxFit.FitWidth:
                if (outputSize.Width / outputSize.Height > inputSize.Width / inputSize.Height)
                {
                    sourceSize = new Size(inputSize.Width, inputSize.Width * outputSize.Height / outputSize.Width);
                    destinationSize = outputSize;
                }
                else
                {
                    sourceSize = inputSize;
                    destinationSize = new Size(
                        outputSize.Width,
                        sourceSize.Height * outputSize.Width / sourceSize.Width);
                }

                break;

            case BoxFit.FitHeight:
                if (outputSize.Width / outputSize.Height > inputSize.Width / inputSize.Height)
                {
                    sourceSize = inputSize;
                    destinationSize = new Size(
                        sourceSize.Width * outputSize.Height / sourceSize.Height,
                        outputSize.Height);
                }
                else
                {
                    sourceSize = new Size(inputSize.Height * outputSize.Width / outputSize.Height, inputSize.Height);
                    destinationSize = outputSize;
                }

                break;

            case BoxFit.None:
                sourceSize = new Size(
                    Math.Min(inputSize.Width, outputSize.Width),
                    Math.Min(inputSize.Height, outputSize.Height));
                destinationSize = sourceSize;
                break;

            case BoxFit.ScaleDown:
                sourceSize = inputSize;
                destinationSize = inputSize;
                var aspectRatio = inputSize.Width / inputSize.Height;

                if (destinationSize.Height > outputSize.Height)
                {
                    destinationSize = new Size(outputSize.Height * aspectRatio, outputSize.Height);
                }

                if (destinationSize.Width > outputSize.Width)
                {
                    destinationSize = new Size(outputSize.Width, outputSize.Width / aspectRatio);
                }

                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(fit), fit, null);
        }

        return new FittedSizes(sourceSize, destinationSize);
    }
}
