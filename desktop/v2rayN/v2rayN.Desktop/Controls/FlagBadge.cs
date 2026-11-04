namespace v2rayN.Desktop.Controls;

/// <summary>
/// RockBlack UI 3.0 server row icon: a small rounded flag drawn from an ISO-3166 alpha-2 code
/// (Avalonia port of the WPF FlagBadge). Windows and many Linux desktops do not render
/// regional-indicator flag emoji, so flags are painted (simplified geometry, fine at 28 px).
/// Unknown codes show the code on a neutral badge; an empty code shows a globe.
/// </summary>
public class FlagBadge : Control
{
    public static readonly StyledProperty<string?> CountryCodeProperty =
        AvaloniaProperty.Register<FlagBadge, string?>(nameof(CountryCode));

    public static readonly StyledProperty<double> CornerRadiusProperty =
        AvaloniaProperty.Register<FlagBadge, double>(nameof(CornerRadius), 6d);

    static FlagBadge()
    {
        AffectsRender<FlagBadge>(CountryCodeProperty, CornerRadiusProperty);
    }

    public string? CountryCode
    {
        get => GetValue(CountryCodeProperty);
        set => SetValue(CountryCodeProperty, value);
    }

    public double CornerRadius
    {
        get => GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }

    private enum Kind
    { H, V, NordicCross, CenterCross, Disc, Custom }

    private sealed record Spec(Kind Kind, Color[] Colors, double[]? Weights = null);

    private static Color C(string hex) => Color.Parse(hex);

    private static readonly Dictionary<string, Spec> Flags = new(StringComparer.OrdinalIgnoreCase)
    {
        // horizontal bands
        ["de"] = new(Kind.H, [C("#000000"), C("#DD0000"), C("#FFCE00")]),
        ["nl"] = new(Kind.H, [C("#AE1C28"), C("#FFFFFF"), C("#21468B")]),
        ["ru"] = new(Kind.H, [C("#FFFFFF"), C("#0039A6"), C("#D52B1E")]),
        ["at"] = new(Kind.H, [C("#ED2939"), C("#FFFFFF"), C("#ED2939")]),
        ["lv"] = new(Kind.H, [C("#9E3039"), C("#FFFFFF"), C("#9E3039")], [2, 1, 2]),
        ["ee"] = new(Kind.H, [C("#0072CE"), C("#000000"), C("#FFFFFF")]),
        ["lt"] = new(Kind.H, [C("#FDB913"), C("#006A44"), C("#C1272D")]),
        ["pl"] = new(Kind.H, [C("#FFFFFF"), C("#DC143C")]),
        ["ua"] = new(Kind.H, [C("#0057B7"), C("#FFD700")]),
        ["hu"] = new(Kind.H, [C("#CD2A3E"), C("#FFFFFF"), C("#436F4D")]),
        ["bg"] = new(Kind.H, [C("#FFFFFF"), C("#00966E"), C("#D62612")]),
        ["lu"] = new(Kind.H, [C("#EF3340"), C("#FFFFFF"), C("#00A3E0")]),
        ["am"] = new(Kind.H, [C("#D90012"), C("#0033A0"), C("#F2A800")]),
        ["id"] = new(Kind.H, [C("#FF0000"), C("#FFFFFF")]),
        ["by"] = new(Kind.H, [C("#C8313E"), C("#4AA657")], [2, 1]),
        ["sk"] = new(Kind.H, [C("#FFFFFF"), C("#0B4EA2"), C("#EE1C25")]),
        ["si"] = new(Kind.H, [C("#FFFFFF"), C("#0000FF"), C("#FF0000")]),
        ["rs"] = new(Kind.H, [C("#C6363C"), C("#0C4076"), C("#FFFFFF")]),
        ["hr"] = new(Kind.H, [C("#FF0000"), C("#FFFFFF"), C("#171796")]),
        ["co"] = new(Kind.H, [C("#FCD116"), C("#003893"), C("#CE1126")], [2, 1, 1]),
        ["es"] = new(Kind.H, [C("#AA151B"), C("#F1BF00"), C("#AA151B")], [1, 2, 1]),
        ["th"] = new(Kind.H, [C("#A51931"), C("#F4F5F8"), C("#2D2A4A"), C("#F4F5F8"), C("#A51931")], [1, 1, 2, 1, 1]),
        ["ar"] = new(Kind.H, [C("#74ACDF"), C("#FFFFFF"), C("#74ACDF")]),
        ["eg"] = new(Kind.H, [C("#CE1126"), C("#FFFFFF"), C("#000000")]),
        ["in"] = new(Kind.H, [C("#FF9933"), C("#FFFFFF"), C("#138808")]),
        ["ge"] = new(Kind.CenterCross, [C("#FFFFFF"), C("#FF0000")]),
        ["il"] = new(Kind.H, [C("#FFFFFF"), C("#0038B8"), C("#FFFFFF"), C("#FFFFFF"), C("#0038B8"), C("#FFFFFF")], [1, 1, 2, 2, 1, 1]),
        ["sg"] = new(Kind.H, [C("#EF3340"), C("#FFFFFF")]),
        ["kz"] = new(Kind.Disc, [C("#00AFCA"), C("#FEC50C")]),
        ["az"] = new(Kind.H, [C("#0092BC"), C("#E4002B"), C("#00AF66")]),
        ["uz"] = new(Kind.H, [C("#0099B5"), C("#FFFFFF"), C("#1EB53A")]),
        ["kg"] = new(Kind.Disc, [C("#E8112D"), C("#FFEF00")]),
        ["md"] = new(Kind.V, [C("#003DA5"), C("#FFD200"), C("#CC092F")]),
        ["ro"] = new(Kind.V, [C("#002B7F"), C("#FCD116"), C("#CE1126")]),
        // vertical bands
        ["fr"] = new(Kind.V, [C("#002395"), C("#FFFFFF"), C("#ED2939")]),
        ["it"] = new(Kind.V, [C("#009246"), C("#FFFFFF"), C("#CE2B37")]),
        ["be"] = new(Kind.V, [C("#000000"), C("#FDDA24"), C("#EF3340")]),
        ["ie"] = new(Kind.V, [C("#169B62"), C("#FFFFFF"), C("#FF883E")]),
        ["mx"] = new(Kind.V, [C("#006847"), C("#FFFFFF"), C("#CE1126")]),
        ["pt"] = new(Kind.V, [C("#006600"), C("#FF0000")], [2, 3]),
        ["ca"] = new(Kind.Custom, [C("#FF0000"), C("#FFFFFF")]),
        // crosses
        ["se"] = new(Kind.NordicCross, [C("#006AA7"), C("#FECC00")]),
        ["fi"] = new(Kind.NordicCross, [C("#FFFFFF"), C("#002F6C")]),
        ["dk"] = new(Kind.NordicCross, [C("#C8102E"), C("#FFFFFF")]),
        ["no"] = new(Kind.NordicCross, [C("#BA0C2F"), C("#00205B"), C("#FFFFFF")]),
        ["is"] = new(Kind.NordicCross, [C("#02529C"), C("#DC1E35"), C("#FFFFFF")]),
        ["ch"] = new(Kind.CenterCross, [C("#DA291C"), C("#FFFFFF")]),
        // discs
        ["jp"] = new(Kind.Disc, [C("#FFFFFF"), C("#BC002D")]),
        ["kr"] = new(Kind.Disc, [C("#FFFFFF"), C("#CD2E3A")]),
        ["vn"] = new(Kind.Disc, [C("#DA251D"), C("#FFFF00")]),
        ["hk"] = new(Kind.Disc, [C("#DE2910"), C("#FFFFFF")]),
        ["tw"] = new(Kind.Custom, [C("#FE0000"), C("#000095"), C("#FFFFFF")]),
        // custom
        ["us"] = new(Kind.Custom, [C("#B22234"), C("#FFFFFF"), C("#3C3B6E")]),
        ["gb"] = new(Kind.Custom, [C("#012169"), C("#FFFFFF"), C("#C8102E")]),
        ["uk"] = new(Kind.Custom, [C("#012169"), C("#FFFFFF"), C("#C8102E")]),
        ["au"] = new(Kind.Custom, [C("#012169"), C("#FFFFFF"), C("#E4002B")]),
        ["tr"] = new(Kind.Custom, [C("#E30A17"), C("#FFFFFF")]),
        ["br"] = new(Kind.Custom, [C("#009C3B"), C("#FFDF00"), C("#002776")]),
        ["ae"] = new(Kind.Custom, [C("#00732F"), C("#FFFFFF"), C("#000000"), C("#FF0000")]),
        ["za"] = new(Kind.H, [C("#E03C31"), C("#FFFFFF"), C("#007749"), C("#FFFFFF"), C("#001489")], [3, 1, 2, 1, 3]),
        ["gr"] = new(Kind.Custom, [C("#0D5EAF"), C("#FFFFFF")]),
        ["cz"] = new(Kind.Custom, [C("#FFFFFF"), C("#D7141A"), C("#11457E")]),
        ["my"] = new(Kind.H, [C("#CC0001"), C("#FFFFFF"), C("#CC0001"), C("#FFFFFF"), C("#CC0001"), C("#FFFFFF"), C("#CC0001")]),
        ["cl"] = new(Kind.H, [C("#FFFFFF"), C("#D52B1E")]),
    };

    protected override Size MeasureOverride(Size availableSize)
    {
        var w = double.IsNaN(Width) ? 28 : Width;
        var h = double.IsNaN(Height) ? 28 : Height;
        return new Size(w, h);
    }

    private IBrush Res(string key, IBrush fallback)
    {
        return this.TryFindResource(key, ActualThemeVariant, out var value) && value is IBrush brush ? brush : fallback;
    }

    public override void Render(DrawingContext dc)
    {
        var w = Bounds.Width;
        var h = Bounds.Height;
        if (w <= 0 || h <= 0)
        {
            return;
        }

        var outer = new Rect(0, 0, w, h);
        var code = (CountryCode ?? string.Empty).Trim().ToLowerInvariant();
        var neutral = Res("RB.Surface2", Brushes.DimGray);
        var muted = Res("RB.TextMuted", Brushes.Gray);

        if (code.Length == 0 || !Flags.TryGetValue(code, out var spec))
        {
            dc.DrawRectangle(neutral, null, outer, CornerRadius, CornerRadius);
            if (code.Length == 0)
            {
                DrawGlobe(dc, w, h, muted);
            }
            else
            {
                var ft = new FormattedText(code.ToUpperInvariant(), CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                    new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.SemiBold), h * 0.38, muted);
                dc.DrawText(ft, new Point((w - ft.Width) / 2, (h - ft.Height) / 2));
            }
            return;
        }

        // the flag area is a 4:3 rectangle centred in the square, clipped to rounded corners
        var fh = Math.Round(h * 0.78);
        var flag = new Rect(0, (h - fh) / 2, w, fh);
        using (dc.PushClip(new RoundedRect(flag, CornerRadius * 0.8)))
        {
            DrawFlag(dc, code, spec, flag);
        }
        // hairline so white flags stay visible on light cards
        var edge = new Pen(new SolidColorBrush(Color.FromArgb(0x30, 0x80, 0x80, 0x80)), 1);
        dc.DrawRectangle(null, edge, new Rect(flag.X + 0.5, flag.Y + 0.5, flag.Width - 1, flag.Height - 1), CornerRadius * 0.8, CornerRadius * 0.8);
    }

    private static SolidColorBrush B(Color c) => new(c);

    private static void Bands(DrawingContext dc, Rect r, Color[] colors, double[]? weights, bool horizontal)
    {
        var total = weights?.Sum() ?? colors.Length;
        var pos = 0d;
        for (var i = 0; i < colors.Length; i++)
        {
            var part = (weights?[i] ?? 1) / total;
            var band = horizontal
                ? new Rect(r.X, r.Y + (r.Height * pos), r.Width, (r.Height * part) + 0.6)
                : new Rect(r.X + (r.Width * pos), r.Y, (r.Width * part) + 0.6, r.Height);
            dc.DrawRectangle(B(colors[i]), null, band);
            pos += part;
        }
    }

    private static void DrawFlag(DrawingContext dc, string code, Spec spec, Rect r)
    {
        var c = spec.Colors;
        switch (spec.Kind)
        {
            case Kind.H:
                Bands(dc, r, c, spec.Weights, true);
                break;

            case Kind.V:
                Bands(dc, r, c, spec.Weights, false);
                break;

            case Kind.NordicCross:
            {
                dc.DrawRectangle(B(c[0]), null, r);
                var cx = r.X + (r.Width * 0.36);
                var cy = r.Y + (r.Height / 2);
                if (c.Length > 2)
                {
                    var t2 = r.Height * 0.32;
                    dc.DrawRectangle(B(c[2]), null, new Rect(cx - (t2 / 2), r.Y, t2, r.Height));
                    dc.DrawRectangle(B(c[2]), null, new Rect(r.X, cy - (t2 / 2), r.Width, t2));
                }
                var t = r.Height * (c.Length > 2 ? 0.16 : 0.22);
                dc.DrawRectangle(B(c[1]), null, new Rect(cx - (t / 2), r.Y, t, r.Height));
                dc.DrawRectangle(B(c[1]), null, new Rect(r.X, cy - (t / 2), r.Width, t));
                break;
            }
            case Kind.CenterCross:
            {
                dc.DrawRectangle(B(c[0]), null, r);
                var cx = r.X + (r.Width / 2);
                var cy = r.Y + (r.Height / 2);
                var t = r.Height * 0.2;
                var len = code == "ch" ? r.Height * 0.62 : double.MaxValue;
                var lx = Math.Min(len, r.Width);
                var ly = Math.Min(len, r.Height);
                dc.DrawRectangle(B(c[1]), null, new Rect(cx - (t / 2), cy - (ly / 2), t, ly));
                dc.DrawRectangle(B(c[1]), null, new Rect(cx - (lx / 2), cy - (t / 2), lx, t));
                break;
            }
            case Kind.Disc:
            {
                dc.DrawRectangle(B(c[0]), null, r);
                var center = new Point(r.X + (r.Width / 2), r.Y + (r.Height / 2));
                var rad = r.Height * (code is "hk" or "vn" ? 0.24 : 0.3);
                dc.DrawEllipse(B(c[1]), null, center, rad, rad);
                break;
            }
            case Kind.Custom:
                DrawCustom(dc, code, c, r);
                break;
        }
    }

    private static StreamGeometry Polygon(params Point[] points)
    {
        var g = new StreamGeometry();
        using var ctx = g.Open();
        ctx.BeginFigure(points[0], true);
        foreach (var p in points.Skip(1))
        {
            ctx.LineTo(p);
        }
        ctx.EndFigure(true);
        return g;
    }

    private static void DrawCustom(DrawingContext dc, string code, Color[] c, Rect r)
    {
        switch (code)
        {
            case "us":
            {
                var colors = Enumerable.Range(0, 13).Select(i => i % 2 == 0 ? c[0] : c[1]).ToArray();
                Bands(dc, r, colors, null, true);
                dc.DrawRectangle(B(c[2]), null, new Rect(r.X, r.Y, r.Width * 0.45, r.Height * 7 / 13));
                break;
            }
            case "gb" or "uk":
                DrawUnion(dc, r, c);
                break;

            case "au":
            {
                dc.DrawRectangle(B(c[0]), null, r);
                DrawUnion(dc, new Rect(r.X, r.Y, r.Width / 2, r.Height / 2), [c[0], c[1], C("#C8102E")]);
                var star = B(c[1]);
                dc.DrawEllipse(star, null, new Point(r.X + (r.Width * 0.74), r.Y + (r.Height * 0.3)), r.Height * 0.07, r.Height * 0.07);
                dc.DrawEllipse(star, null, new Point(r.X + (r.Width * 0.82), r.Y + (r.Height * 0.62)), r.Height * 0.07, r.Height * 0.07);
                dc.DrawEllipse(star, null, new Point(r.X + (r.Width * 0.64), r.Y + (r.Height * 0.72)), r.Height * 0.07, r.Height * 0.07);
                dc.DrawEllipse(star, null, new Point(r.X + (r.Width * 0.25), r.Y + (r.Height * 0.76)), r.Height * 0.1, r.Height * 0.1);
                break;
            }
            case "tr":
            {
                dc.DrawRectangle(B(c[0]), null, r);
                var cy = r.Y + (r.Height / 2);
                dc.DrawEllipse(B(c[1]), null, new Point(r.X + (r.Width * 0.38), cy), r.Height * 0.27, r.Height * 0.27);
                dc.DrawEllipse(B(c[0]), null, new Point(r.X + (r.Width * 0.43), cy), r.Height * 0.21, r.Height * 0.21);
                dc.DrawEllipse(B(c[1]), null, new Point(r.X + (r.Width * 0.66), cy), r.Height * 0.08, r.Height * 0.08);
                break;
            }
            case "ca":
            {
                Bands(dc, r, [c[0], c[1], c[0]], [1, 2, 1], false);
                var cx = r.X + (r.Width / 2);
                var cy = r.Y + (r.Height / 2);
                var s = r.Height * 0.22;
                dc.DrawGeometry(B(c[0]), null, Polygon(
                    new Point(cx, cy - (s * 1.3)), new Point(cx + s, cy - (s * 0.1)), new Point(cx + (s * 0.6), cy + (s * 0.7)),
                    new Point(cx + (s * 0.1), cy + (s * 0.6)), new Point(cx + (s * 0.1), cy + (s * 1.2)), new Point(cx - (s * 0.1), cy + (s * 1.2)),
                    new Point(cx - (s * 0.1), cy + (s * 0.6)), new Point(cx - (s * 0.6), cy + (s * 0.7)), new Point(cx - s, cy - (s * 0.1))));
                break;
            }
            case "br":
            {
                dc.DrawRectangle(B(c[0]), null, r);
                var cx = r.X + (r.Width / 2);
                var cy = r.Y + (r.Height / 2);
                dc.DrawGeometry(B(c[1]), null, Polygon(
                    new Point(cx, r.Y + (r.Height * 0.1)), new Point(r.Right - (r.Width * 0.08), cy),
                    new Point(cx, r.Bottom - (r.Height * 0.1)), new Point(r.X + (r.Width * 0.08), cy)));
                dc.DrawEllipse(B(c[2]), null, new Point(cx, cy), r.Height * 0.22, r.Height * 0.22);
                break;
            }
            case "ae":
            {
                var rest = new Rect(r.X + (r.Width * 0.27), r.Y, r.Width * 0.73, r.Height);
                Bands(dc, rest, [c[0], c[1], c[2]], null, true);
                dc.DrawRectangle(B(c[3]), null, new Rect(r.X, r.Y, (r.Width * 0.27) + 0.5, r.Height));
                break;
            }
            case "gr":
            {
                var colors = Enumerable.Range(0, 9).Select(i => i % 2 == 0 ? c[0] : c[1]).ToArray();
                Bands(dc, r, colors, null, true);
                var canton = new Rect(r.X, r.Y, r.Height * 5 / 9, r.Height * 5 / 9);
                dc.DrawRectangle(B(c[0]), null, canton);
                var t = canton.Height / 5;
                dc.DrawRectangle(B(c[1]), null, new Rect(canton.X + (canton.Width / 2) - (t / 2), canton.Y, t, canton.Height));
                dc.DrawRectangle(B(c[1]), null, new Rect(canton.X, canton.Y + (canton.Height / 2) - (t / 2), canton.Width, t));
                break;
            }
            case "cz":
            {
                Bands(dc, r, [c[0], c[1]], null, true);
                dc.DrawGeometry(B(c[2]), null, Polygon(new Point(r.X, r.Y), new Point(r.X + (r.Width / 2), r.Y + (r.Height / 2)), new Point(r.X, r.Bottom)));
                break;
            }
            case "tw":
            {
                dc.DrawRectangle(B(c[0]), null, r);
                dc.DrawRectangle(B(c[1]), null, new Rect(r.X, r.Y, r.Width / 2, r.Height / 2));
                dc.DrawEllipse(B(c[2]), null, new Point(r.X + (r.Width / 4), r.Y + (r.Height / 4)), r.Height * 0.12, r.Height * 0.12);
                break;
            }
        }
    }

    private static void DrawUnion(DrawingContext dc, Rect r, Color[] c)
    {
        dc.DrawRectangle(B(c[0]), null, r);
        using (dc.PushClip(r))
        {
            var white = new Pen(B(c[1]), r.Height * 0.2);
            var red = new Pen(B(c[2]), r.Height * 0.08);
            dc.DrawLine(white, r.TopLeft, r.BottomRight);
            dc.DrawLine(white, r.BottomLeft, r.TopRight);
            dc.DrawLine(red, r.TopLeft, r.BottomRight);
            dc.DrawLine(red, r.BottomLeft, r.TopRight);
            var cx = r.X + (r.Width / 2);
            var cy = r.Y + (r.Height / 2);
            var tw = r.Height * 0.33;
            var tr = r.Height * 0.2;
            dc.DrawRectangle(B(c[1]), null, new Rect(cx - (tw / 2), r.Y, tw, r.Height));
            dc.DrawRectangle(B(c[1]), null, new Rect(r.X, cy - (tw / 2), r.Width, tw));
            dc.DrawRectangle(B(c[2]), null, new Rect(cx - (tr / 2), r.Y, tr, r.Height));
            dc.DrawRectangle(B(c[2]), null, new Rect(r.X, cy - (tr / 2), r.Width, tr));
        }
    }

    private static void DrawGlobe(DrawingContext dc, double w, double h, IBrush brush)
    {
        var pen = new Pen(brush, 1.4);
        var c = new Point(w / 2, h / 2);
        var rad = Math.Min(w, h) * 0.3;
        dc.DrawEllipse(null, pen, c, rad, rad);
        dc.DrawEllipse(null, pen, c, rad * 0.45, rad);
        dc.DrawLine(pen, new Point(c.X - rad, c.Y), new Point(c.X + rad, c.Y));
    }
}
