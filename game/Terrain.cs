using Godot;

namespace Proximity.Game;

public partial class Terrain : StaticBody3D
{
    [Export] public MeshInstance3D MeshInstance { get; set; }
    [Export] public CollisionShape3D CollisionShape { get; set; }

    public override async void _Ready()
    {
        var material = (ShaderMaterial) MeshInstance.GetActiveMaterial(0);
        var noiseTex = material.GetShaderParameter("noise_tex").As<NoiseTexture2D>();
        float heightScale = material.GetShaderParameter("height_scale").AsSingle();

        while (noiseTex.GetImage() is null)
        {
            await ToSignal(noiseTex, Resource.SignalName.Changed);
        }

        var img = noiseTex.GetImage();
        var mesh = (PlaneMesh) MeshInstance.Mesh;
        int mapWidth = mesh.SubdivideWidth + 2;
        int mapDepth = mesh.SubdivideDepth + 2;
        float[] mapData = new float[mapWidth * mapDepth];

        for (int z = 0; z < mapDepth; z++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                float u = (float) x / (mapWidth - 1);
                float v = (float) z / (mapDepth - 1);
                mapData[x + z * mapWidth] = heightScale * (GetBilinearPixel(img, u, v) - 0.5f);
            }
        }

        CollisionShape.Shape = new HeightMapShape3D
        {
            MapWidth = mapWidth,
            MapDepth = mapDepth,
            MapData = mapData,
        };

        CollisionShape.Scale = new Vector3(mesh.Size.X / (mapWidth - 1), 1f, mesh.Size.Y / (mapDepth - 1));
    }

    private static float GetBilinearPixel(Image img, float u, float v)
    {
        float x = u * (img.GetWidth() - 1);
        float y = v * (img.GetHeight() - 1);

        int x0 = Mathf.FloorToInt(x);
        int x1 = Mathf.Min(x0 + 1, img.GetWidth() - 1);
        int y0 = Mathf.FloorToInt(y);
        int y1 = Mathf.Min(y0 + 1, img.GetHeight() - 1);

        float tx = x - x0;
        float ty = y - y0;

        float p00 = img.GetPixel(x0, y0).R;
        float p10 = img.GetPixel(x1, y0).R;
        float p01 = img.GetPixel(x0, y1).R;
        float p11 = img.GetPixel(x1, y1).R;

        float top = Mathf.Lerp(p00, p10, tx);
        float bottom = Mathf.Lerp(p01, p11, tx);

        return Mathf.Lerp(top, bottom, ty);
    }
}
