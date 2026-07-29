using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix;

// Dart parity source: dart_sample/lib/demos/general/composited_transform_demo_page.dart (exact sample parity)

public sealed class CompositedTransformDemoPage : StatefulWidget
{
    public override State CreateState() => new CompositedTransformDemoPageState();

    private sealed class CompositedTransformDemoPageState : State
    {
        private readonly LayerLink _link = new();
        private double _targetLeft = 48;
        private bool _showTarget = true;
        private bool _showWhenUnlinked = true;

        public override Widget Build(BuildContext context)
        {
            return new Column(
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                spacing: 12,
                children:
                [
                    new Text("CompositedTransformTarget + Follower", fontSize: 20, color: Colors.Black),
                    new Text(
                        "The blue follower is painted in a separate composited layer. Its top-center stays 12 px " +
                        "below the orange target's bottom-center. Both labels also expose typed layer annotations.",
                        fontSize: 14,
                        color: Colors.DimGray),
                    BuildPreview(),
                    new Wrap(
                        spacing: 8,
                        runSpacing: 8,
                        children:
                        [
                            new OutlinedButton(
                                onPressed: () => SetState(() => _targetLeft = Math.Max(16, _targetLeft - 36)),
                                child: new Text("Move left")),
                            new OutlinedButton(
                                onPressed: () => SetState(() => _targetLeft = Math.Min(224, _targetLeft + 36)),
                                child: new Text("Move right")),
                            new OutlinedButton(
                                onPressed: () => SetState(() => _showTarget = !_showTarget),
                                child: new Text(_showTarget ? "Remove target" : "Restore target")),
                            new OutlinedButton(
                                onPressed: () => SetState(() => _showWhenUnlinked = !_showWhenUnlinked),
                                child: new Text(
                                    _showWhenUnlinked ? "Unlinked: visible" : "Unlinked: hidden")),
                        ]),
                    new Text(
                        $"target={(_showTarget ? $"x={_targetLeft:0}" : "removed")}; " +
                        $"showWhenUnlinked={_showWhenUnlinked.ToString().ToLowerInvariant()}",
                        fontSize: 13,
                        color: Color.Parse("#FF334155")),
                ]);
        }

        private Widget BuildPreview()
        {
            var children = new List<Widget>();
            if (_showTarget)
            {
                children.Add(new Positioned(
                    left: _targetLeft,
                    top: 36,
                    width: 88,
                    height: 52,
                    child: new CompositedTransformTarget(
                        _link,
                        child: new AnnotatedRegion<string>(
                            value: "target",
                            child: BuildLabel("TARGET", Color.Parse("#FFF59E0B"))))));
            }

            children.Add(new Positioned(
                left: 0,
                top: 0,
                width: 120,
                height: 48,
                child: new CompositedTransformFollower(
                    _link,
                    showWhenUnlinked: _showWhenUnlinked,
                    offset: new Vector(0, 12),
                    targetAnchor: Alignment.BottomCenter,
                    followerAnchor: Alignment.TopCenter,
                    child: new AnnotatedRegion<string>(
                        value: "follower",
                        child: BuildLabel("FOLLOWER", Color.Parse("#FF2563EB"), Colors.White)))));

            return new Container(
                height: 190,
                color: Color.Parse("#FFF1F5F9"),
                child: new Stack(clipBehavior: Clip.None, children: children));
        }

        private static Widget BuildLabel(string label, Color color, Color? textColor = null)
        {
            return new Container(
                color: color,
                alignment: Alignment.Center,
                child: new Text(label, fontSize: 13, color: textColor ?? Colors.Black));
        }
    }
}
