using UnityEngine;
using XNoise;

namespace LibNoise
{
    public class Image : SerializableModuleBase
    {
        public Texture2D input;
        public RenderTexture rdb;

        public Image(int count) : base(count) { }

        public override RenderTexture GetValueGPU(GPURenderingDatas renderingDatas)
        {
            _materialGPU = XNoiseShaderCache.GetMaterial(XNoiseShaderPaths.ReadImage);
            _materialGPU.SetTexture("_TextureA", input);

            return GetImage(_materialGPU, renderingDatas);
        }

        public override double GetValueCPU(double x, double y, double z)
        {
            Vector2 lnlat = CoordinatesProjector.GetLnLatFromPosition(new Vector3((float)x, (float)y, (float)z));
            Vector2 uvs = new Vector2((lnlat.x + 180f) / 360f, (lnlat.y + 90f) / 180f);

            return input.GetPixel((int)((float)input.width * uvs.x), (int)((float)input.height * uvs.y)).r -.5f;
        }
    }
}