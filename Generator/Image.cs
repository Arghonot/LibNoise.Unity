using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static XNode.Node;

namespace LibNoise
{
    public class Image : SerializableModuleBase
    {
        public Texture2D input;
        private Shader _sphericalGPUShader = Shader.Find("Xnoise/Modifiers/ReadImage");
        public Material _materialGPU;
        public RenderTexture rdb;

        public Image(int count) : base(count)
        {
            _materialGPU = new Material(_sphericalGPUShader);
        }

        public override RenderTexture GetValueGPU(Vector2 size, RenderingAreaData area, Vector3 origin, ProjectionType projection = ProjectionType.Flat)
        {
            UnityEngine.Debug.Log("Image spherical value");
            _materialGPU.SetTexture("_TextureA", input);

            return GetImage(_materialGPU, size);
        }

        public override double GetValueCPU(double x, double y, double z)
        {
            Vector2 lnlat = CoordinatesProjector.GetLnLatFromPosition(new Vector3((float)x, (float)y, (float)z));
            Vector2 uvs = new Vector2((lnlat.x + 180f) / 360f, (lnlat.y + 90f) / 180f);

            return input.GetPixel((int)((float)input.width * uvs.x), (int)((float)input.height * uvs.y)).r -.5f;
        }
    }
}