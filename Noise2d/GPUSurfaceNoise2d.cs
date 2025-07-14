using LibNoise;
using System;
using UnityEngine;
using XNoise;

namespace LibNoise
{
    public class GPUSurfaceNoise2d : Noise2d
    {
        /// <summary>
        /// Class contening all the datas necessary for a GPU rendering.
        /// Aim at allowing the GPU to operate with as much flexibility than CPU.
        /// </summary>
        public class GPURenderingDatas
        {
            public Vector3 origin = Vector3.zero;
            public Vector3 scale = Vector3.one;
            public Vector3 rotation;
            public RenderTexture displacementMap;
            public float turbulencePower;
            public Vector4 quaternionRotation
            {
                get
                {
                    Quaternion quat = Quaternion.Euler(rotation);
                    return new Vector4(quat.x, quat.y, quat.z, quat.w);
                }
            }
            public RenderingAreaData area { get { return _area; } }
            public ProjectionType projection { get { return _projection; } }
            public Vector2 size { get { return _size; } }

            private RenderingAreaData _area;
            private ProjectionType _projection;
            private Vector2 _size;

            private void GetBlackTexture()
            {
                displacementMap = new RenderTexture((int)size.x, (int)size.y, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
                Graphics.Blit(Texture2D.blackTexture, displacementMap);
                //displacementMap = new Texture2D((int)_size.x, (int)size.y);
                //UnityEngine.Color[] pixels = Enumerable.Repeat(UnityEngine.Color.black, displacementMap.width * displacementMap.height).ToArray();
                //displacementMap.SetPixels(pixels);
                //displacementMap.Apply();
            }

            public GPURenderingDatas(Vector2 finalTextureSize, ProjectionType type, RenderingAreaData area)
            {
                this._area = area;
                this._projection = type;
                this._size = finalTextureSize;
                this.origin = Vector3.one;
                this.rotation = Vector3.zero;
                GetBlackTexture();
            }
        }

        #region Fields

        public bool useGPU = false;
        public Vector3 origin;
        public RenderTexture renderTexture { get => _renderedTexture; }
        private RenderTexture _renderedTexture;
        private GPURenderingDatas datas;

        #endregion

        #region Constructors


        protected GPUSurfaceNoise2d() : base() { }


        public GPUSurfaceNoise2d(int size)
            : base(size, size, null) { }

        public GPUSurfaceNoise2d(int size, SerializableModuleBase generator) : base(size, size, generator) { }


        public GPUSurfaceNoise2d(int width, int height, SerializableModuleBase generator = null) : base(width, height, generator) { }

        #endregion

        #region Methods

        /// <summary>
        /// Clears the noise map.
        /// </summary>
        /// <param name="value">The constant value to clear the noise map with.</param>
        public void Clear(float value = 0f)
        {
            // implement
        }

        /// <summary>
        /// Generates a planar projection of a point in the noise map.
        /// </summary>
        /// <param name="x">The position on the x-axis.</param>
        /// <param name="y">The position on the y-axis.</param>
        /// <returns>The corresponding noise map value.</returns>
        private double GeneratePlanar(double x, double y)
        {

            return _generator.GetValueCPU(x, 0.0, y);
        }

        /// <summary>
        /// Generates a non-seamless planar projection of the noise map.
        /// </summary>
        /// <param name="left">The clip region to the left.</param>
        /// <param name="right">The clip region to the right.</param>
        /// <param name="top">The clip region to the top.</param>
        /// <param name="bottom">The clip region to the bottom.</param>
        /// <param name="isSeamless">Indicates whether the resulting noise map should be seamless.</param>
        public void GeneratePlanar(double left, double right, double top, double bottom, bool isSeamless = true, Texture2D texture2d = null)
        {
            base.GeneratePlanar(left, right, top, bottom, isSeamless);
            // set texture here
            datas = new GPURenderingDatas(new Vector2(Width, Height), ProjectionType.Flat, RenderingAreaData.standardCartesian);

            if (texture2d != null)
            {
                RenderTexture rt = new RenderTexture(texture2d.width / 2, texture2d.height / 2, 0);
                RenderTexture.active = rt;
                // Copy your texture ref to the render texture
                Graphics.Blit(texture2d, rt);
                datas.displacementMap = rt;
            }
            //datas.origin = origin;
            // set texture here
            _renderedTexture = _generator.GetValueGPU(datas);
        }

        /// <summary>
        /// Generates a cylindrical projection of a point in the noise map.
        /// </summary>
        /// <param name="angle">The angle of the point.</param>
        /// <param name="height">The height of the point.</param>
        /// <returns>The corresponding noise map value.</returns>
        private double GenerateCylindrical(double angle, double height)
        {
            var x = Math.Cos(angle * Mathf.Deg2Rad);
            var y = height;
            var z = Math.Sin(angle * Mathf.Deg2Rad);
            return _generator.GetValueCPU(x, y, z);
        }

        /// <summary>
        /// Generates a cylindrical projection of the noise map.
        /// </summary>
        /// <param name="angleMin">The maximum angle of the clip region.</param>
        /// <param name="angleMax">The minimum angle of the clip region.</param>
        /// <param name="heightMin">The minimum height of the clip region.</param>
        /// <param name="heightMax">The maximum height of the clip region.</param>
        public void GenerateCylindrical(double angleMin, double angleMax, double heightMin, double heightMax)
        {
            base.GenerateCylindrical(angleMin, angleMax, heightMin, heightMax); 
            GPURenderingDatas datas = new GPURenderingDatas(new Vector2(Width, Height), ProjectionType.Cylindrical, RenderingAreaData.standardCylindrical);
            _renderedTexture = _generator.GetValueGPU(datas);
        }

        /// <summary>
        /// Generates a spherical projection of a point in the noise map.
        /// </summary>
        /// <param name="lat">The latitude of the point.</param>
        /// <param name="lon">The longitude of the point.</param>
        /// <returns>The corresponding noise map value.</returns>
        private double GenerateSpherical(double lat, double lon)
        {
            var r = Math.Cos(Mathf.Deg2Rad * lat);
            return _generator.GetValueCPU(r * Math.Cos(Mathf.Deg2Rad * lon), Math.Sin(Mathf.Deg2Rad * lat),
                r * Math.Sin(Mathf.Deg2Rad * lon));
        }

        Vector2 bounds = new Vector2();
        float distance;

        /// <summary>
        /// Generates a spherical projection of the noise map.
        /// </summary>
        /// <param name="south">The clip region to the south.</param>
        /// <param name="north">The clip region to the north.</param>
        /// <param name="west">The clip region to the west.</param>
        /// <param name="east">The clip region to the east.</param>
        public void GenerateSpherical(double south, double north, double west, double east)
        {
            base.GenerateSpherical(south, north, west, east);
            GPURenderingDatas datas = new GPURenderingDatas(new Vector2(Width, Height), ProjectionType.Spherical, RenderingAreaData.standardSpherical);

            _renderedTexture = _generator.GetValueGPU(datas);
        }

        /// <summary>
        /// Creates a grayscale texture map for the current content of the noise map.
        /// </summary>
        /// <returns>The created texture map.</returns>
        public Texture2D GetTexture()
        {
            RenderTexture.active = _renderedTexture;
            var tex = new Texture2D(_renderedTexture.width, _renderedTexture.height);
            tex.ReadPixels(new Rect(0, 0, _renderedTexture.width, _renderedTexture.height), 0, 0);
            tex.Apply();

            return tex;
        }

        public Texture2D GetFinalizedTexture()
        {
            RenderTexture preview = new RenderTexture(_renderedTexture.width, _renderedTexture.height, 0, RenderTextureFormat.ARGB32);
            RenderTexture.active = preview;
            var mat = XNoiseShaderCache.GetMaterial(XNoiseShaderPaths.Visualizer);

            mat.SetTexture("_Input", _renderedTexture);
            Graphics.Blit(_renderedTexture, preview, mat);
            var tex = new Texture2D(_renderedTexture.width, _renderedTexture.height);
            tex.ReadPixels(new Rect(0, 0, _renderedTexture.width, _renderedTexture.height), 0, 0);
            tex.Apply();

            return tex;
        }

        public RenderTexture getTexture()
        {
            RenderTexture.active = _renderedTexture;

            return _renderedTexture;
        }

        #endregion

        // Where do this even fits ?
        public static RenderTexture GetImage(Material material, GPUSurfaceNoise2d.GPURenderingDatas renderingDatas, bool isGenerator = false)
        {
            if (isGenerator)
            {
                material.SetVector("_Rotation", renderingDatas.quaternionRotation);
                material.SetVector("_OffsetPosition", renderingDatas.origin);
                material.SetFloat("_Radius", 1f);
                material.SetFloat("_TurbulencePower", renderingDatas.turbulencePower);
                material.SetVector("_Scale", renderingDatas.scale);
                material.SetTexture("_TurbulenceMap", renderingDatas.displacementMap);
            }

            RenderTexture rdB = RdbCollection.GetFromStack(renderingDatas.size);

            RenderTexture.active = rdB;
            Graphics.Blit(Texture2D.blackTexture, rdB, material, isGenerator ? (int)renderingDatas.projection : 0);

            RdbCollection.AddToStack(rdB);

            return rdB;
        }

        public static Texture2D duplicateTexture(Texture2D source)
        {
            RenderTexture renderTex = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);

            Graphics.Blit(source, renderTex);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = renderTex;
            Texture2D readableText = new Texture2D(source.width, source.height);
            readableText.ReadPixels(new Rect(0, 0, renderTex.width, renderTex.height), 0, 0);
            readableText.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(renderTex);
            return readableText;
        }
    }
}