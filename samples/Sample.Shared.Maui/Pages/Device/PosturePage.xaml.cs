using System.ComponentModel;
using Microsoft.Maui.Graphics;

namespace Sample.Shared.Maui.Pages.Device;

public partial class PosturePage : ContentPage
{
    readonly PostureDrawable drawable = new();

    public PosturePage()
    {
        this.InitializeComponent();
        this.Map.Drawable = this.drawable;
    }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();
        if (this.BindingContext is PostureViewModel vm)
            vm.PropertyChanged += this.OnViewModelChanged;
    }

    void OnViewModelChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not PostureViewModel vm || e.PropertyName != nameof(PostureViewModel.Current))
            return;

        this.drawable.Posture = vm.Current;
        this.drawable.Window = new SizeF((float)(this.Window?.Width ?? 0), (float)(this.Window?.Height ?? 0));
        this.Map.Invalidate();
    }


    sealed class PostureDrawable : IDrawable
    {
        public ScreenPosture Posture { get; set; } = ScreenPosture.None;
        public SizeF Window { get; set; }

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            if (this.Window.Width <= 0 || this.Window.Height <= 0)
                return;

            var scale = Math.Min(dirtyRect.Width / this.Window.Width, dirtyRect.Height / this.Window.Height);
            var width = this.Window.Width * scale;
            var height = this.Window.Height * scale;
            var left = dirtyRect.Left + (dirtyRect.Width - width) / 2;
            var top = dirtyRect.Top;

            RectF Map(ScreenRect r) => new(
                left + (float)r.X * scale,
                top + (float)r.Y * scale,
                Math.Max(2, (float)r.Width * scale),
                Math.Max(2, (float)r.Height * scale)
            );

            canvas.StrokeColor = Colors.Gray;
            canvas.StrokeSize = 2;
            canvas.DrawRoundedRectangle(left, top, width, height, 8);

            var segments = this.Posture.Segments(new ScreenSize(this.Window.Width, this.Window.Height));
            if (segments is { } s)
            {
                canvas.FillColor = Color.FromRgba(0x3B, 0x82, 0xF6, 0x33);
                canvas.FillRectangle(Map(s.First));
                canvas.FillColor = Color.FromRgba(0x10, 0xB9, 0x81, 0x33);
                canvas.FillRectangle(Map(s.Second));
            }

            if (this.Posture.Fold is { } fold)
            {
                canvas.FillColor = Colors.Red;
                canvas.FillRectangle(Map(fold));
            }

            canvas.FillColor = Colors.Orange;
            foreach (var occlusion in this.Posture.Occlusions)
                canvas.FillRectangle(Map(occlusion));
        }
    }
}
