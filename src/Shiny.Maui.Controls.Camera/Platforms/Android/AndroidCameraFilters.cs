using Android.Graphics;

namespace Shiny.Maui.Controls.Camera;

// Maps the cross-platform CameraFilter enum to an Android ColorMatrix (null = passthrough),
// applied to the live preview via View.SetRenderEffect on API 31+.
static class AndroidCameraFilters
{
    public static ColorMatrix? ColorMatrix(CameraFilter filter)
    {
        switch (filter)
        {
            case CameraFilter.Mono:
            {
                var m = new ColorMatrix();
                m.SetSaturation(0f);
                return m;
            }

            case CameraFilter.Noir:
            {
                var m = new ColorMatrix();
                m.SetSaturation(0f);
                var contrast = new ColorMatrix(new[]
                {
                    1.4f, 0, 0, 0, -40f,
                    0, 1.4f, 0, 0, -40f,
                    0, 0, 1.4f, 0, -40f,
                    0, 0, 0, 1f, 0
                });
                m.PostConcat(contrast);
                return m;
            }

            case CameraFilter.Sepia:
                return new ColorMatrix(new[]
                {
                    0.393f, 0.769f, 0.189f, 0, 0,
                    0.349f, 0.686f, 0.168f, 0, 0,
                    0.272f, 0.534f, 0.131f, 0, 0,
                    0, 0, 0, 1f, 0
                });

            case CameraFilter.Invert:
                return new ColorMatrix(new[]
                {
                    -1f, 0, 0, 0, 255f,
                    0, -1f, 0, 0, 255f,
                    0, 0, -1f, 0, 255f,
                    0, 0, 0, 1f, 0
                });

            case CameraFilter.Vivid:
            {
                var m = new ColorMatrix();
                m.SetSaturation(1.6f);
                return m;
            }

            case CameraFilter.Cool:
                return new ColorMatrix(new[]
                {
                    0.9f, 0, 0, 0, 0,
                    0, 1.0f, 0, 0, 0,
                    0, 0, 1.2f, 0, 10f,
                    0, 0, 0, 1f, 0
                });

            case CameraFilter.Warm:
                return new ColorMatrix(new[]
                {
                    1.2f, 0, 0, 0, 10f,
                    0, 1.0f, 0, 0, 0,
                    0, 0, 0.85f, 0, 0,
                    0, 0, 0, 1f, 0
                });

            case CameraFilter.Fade:
            {
                // slightly desaturate, then lift blacks + drop contrast for a washed look
                var m = new ColorMatrix();
                m.SetSaturation(0.8f);
                m.PostConcat(new ColorMatrix(new[]
                {
                    0.85f, 0, 0, 0, 22f,
                    0, 0.85f, 0, 0, 22f,
                    0, 0, 0.85f, 0, 22f,
                    0, 0, 0, 1f, 0
                }));
                return m;
            }

            case CameraFilter.Chrome:
            {
                // punchy + slightly cool
                var m = new ColorMatrix();
                m.SetSaturation(1.3f);
                m.PostConcat(new ColorMatrix(new[]
                {
                    1.05f, 0, 0, 0, -6f,
                    0, 1.05f, 0, 0, -6f,
                    0, 0, 1.15f, 0, 4f,
                    0, 0, 0, 1f, 0
                }));
                return m;
            }

            case CameraFilter.Instant:
                // warm, low-contrast vintage instant film
                return new ColorMatrix(new[]
                {
                    1.0f, 0.1f, 0.05f, 0, 12f,
                    0.05f, 0.95f, 0.05f, 0, 10f,
                    0.05f, 0.1f, 0.8f, 0, 4f,
                    0, 0, 0, 1f, 0
                });

            case CameraFilter.Tonal:
            {
                // muted, low-contrast black & white
                var m = new ColorMatrix();
                m.SetSaturation(0f);
                m.PostConcat(new ColorMatrix(new[]
                {
                    0.9f, 0, 0, 0, 14f,
                    0, 0.9f, 0, 0, 14f,
                    0, 0, 0.9f, 0, 14f,
                    0, 0, 0, 1f, 0
                }));
                return m;
            }

            default:
                return null;
        }
    }
}
