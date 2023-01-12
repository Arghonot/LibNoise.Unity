using System;
using System.Drawing;
using System.Linq;
using System.Xml.Serialization;
using UnityEngine;

namespace LibNoise
{
    public enum ProjectionType
    {
        Flat,
        Spherical,
        Cylindrical
    }

    public struct RenderingAreaData
    {
        public double left;
        public double right;
        public double top;
        public double bottom;

        public RenderingAreaData(double a, double b, double c, double d)
        {
            left = a;
            right = b;
            top = c;
            bottom = d;
        }

        public static readonly RenderingAreaData standardSpherical = new RenderingAreaData(Noise2D.West, Noise2D.East, Noise2D.North, Noise2D.South);
        public static readonly RenderingAreaData standardCartesian = new RenderingAreaData(Noise2D.Left, Noise2D.Right, Noise2D.North, Noise2D.South);
    }

    /// <summary>
    /// Class contening all the datas necessary for a GPU rendering.
    /// Aim at allowing the GPU to operate with as much flexibility than CPU.
    /// </summary>
    public class GPURenderingDatas
    {
        public Vector3 origin;
        public Vector3 rotation;
        public Texture2D displacementMap;// could be a rendertexture ultimately
        public Vector4 quaternionRotation 
        { 
            get
            {
                Quaternion quat = Quaternion.Euler(rotation);
                Vector4 v4 = new Vector4(quat.x, quat.y, quat.z, quat.w);
                return v4;
            } 
        }
        public RenderingAreaData area { get { return _area; } }
        public ProjectionType projection { get { return _projection; } }
        public Vector2 size { get { return _size; } }

        private RenderingAreaData _area;
        private ProjectionType _projection;
        private Vector2 _size;
        private Vector4 _quaternionRotation;

        private void GetBlackTexture()
        {
            displacementMap = new Texture2D((int)_size.x, (int)size.y);
            UnityEngine.Color[] pixels = Enumerable.Repeat(UnityEngine.Color.black, displacementMap.width * displacementMap.height).ToArray();
            displacementMap.SetPixels(pixels);
            displacementMap.Apply();
        }

        public GPURenderingDatas(Vector2 finalTextureSize, ProjectionType type, RenderingAreaData area)
        {
            this._area = area;
            this._projection = type;
            this._size = finalTextureSize;
            this.origin = Vector3.one;
            this.rotation = Vector3.zero;
            _quaternionRotation = new Vector4(0, 0, 0, 1);
            GetBlackTexture();
        }
    }

    /// <summary>
    /// Provides a two-dimensional noise map.
    /// </summary>
	/// <remarks>This covers most of the functionality from LibNoise's noiseutils library, but 
	/// the method calls might not be the same. See the tutorials project if you're wondering
	/// which calls are equivalent.</remarks>
    public class Noise2D : IDisposable
    {
        #region Constants

        public static readonly double South = -90.0;
        public static readonly double North = 90.0;
        public static readonly double West = -180.0;
        public static readonly double East = 180.0;
        public static readonly double AngleMin = -180.0;
        public static readonly double AngleMax = 180.0;
        public static readonly double Left = -1.0;
        public static readonly double Right = 1.0;
        public static readonly double Top = -1.0;
        public static readonly double Bottom = 1.0;

        #endregion

        #region Fields
        public bool useGPU = false;
        public Vector3 origin;
        private RenderTexture renderedTexture;
        private int _width;
        private int _height;
        private float[,] _data;
        private readonly int _ucWidth;
        private readonly int _ucHeight;
        private int _ucBorder = 1; // Border size of extra noise for uncropped data.

        private readonly float[,] _ucData;
            // Uncropped data. This has a border of extra noise data used for calculating normal map edges.

        private float _borderValue = float.NaN;
        private SerializableModuleBase _generator;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of Noise2D.
        /// </summary>
        protected Noise2D()
        {
        }

        /// <summary>
        /// Initializes a new instance of Noise2D.
        /// </summary>
        /// <param name="size">The width and height of the noise map.</param>
        public Noise2D(int size)
            : this(size, size, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of Noise2D.
        /// </summary>
        /// <param name="size">The width and height of the noise map.</param>
        /// <param name="generator">The generator module.</param>
        public Noise2D(int size, SerializableModuleBase generator)
            : this(size, size, generator)
        {
        }

        /// <summary>
        /// Initializes a new instance of Noise2D.
        /// </summary>
        /// <param name="width">The width of the noise map.</param>
        /// <param name="height">The height of the noise map.</param>
        /// <param name="generator">The generator module.</param>
        public Noise2D(int width, int height, SerializableModuleBase generator = null)
        {
            _generator = generator;
            _width = width;
            _height = height;
            _data = new float[width, height];
            _ucWidth = width + _ucBorder * 2;
            _ucHeight = height + _ucBorder * 2;
            _ucData = new float[width + _ucBorder * 2, height + _ucBorder * 2];
        }

        #endregion

        #region Indexers

        /// <summary>
        /// Gets or sets a value in the noise map by its position.
        /// </summary>
        /// <param name="x">The position on the x-axis.</param>
        /// <param name="y">The position on the y-axis.</param>
        /// <param name="isCropped">Indicates whether to select the cropped (default) or uncropped noise map data.</param>
        /// <returns>The corresponding value.</returns>
        public float this[int x, int y, bool isCropped = true]
        {
            get
            {
                if (isCropped)
                {
                    if (x < 0 && x >= _width)
                    {
                        throw new ArgumentOutOfRangeException("Invalid x position");
                    }
                    if (y < 0 && y >= _height)
                    {
                        throw new ArgumentOutOfRangeException("Invalid y position");
                    }
                    return _data[x, y];
                }
                if (x < 0 && x >= _ucWidth)
                {
                    throw new ArgumentOutOfRangeException("Invalid x position");
                }
                if (y < 0 && y >= _ucHeight)
                {
                    throw new ArgumentOutOfRangeException("Invalid y position");
                }
                return _ucData[x, y];
            }
            set
            {
                if (isCropped)
                {
                    if (x < 0 && x >= _width)
                    {
                        throw new ArgumentOutOfRangeException("Invalid x position");
                    }
                    if (y < 0 && y >= _height)
                    {
                        throw new ArgumentOutOfRangeException("Invalid y position");
                    }
                    _data[x, y] = value;
                }
                else
                {
                    if (x < 0 && x >= _ucWidth)
                    {
                        throw new ArgumentOutOfRangeException("Invalid x position");
                    }
                    if (y < 0 && y >= _ucHeight)
                    {
                        throw new ArgumentOutOfRangeException("Invalid y position");
                    }
                    _ucData[x, y] = value;
                }
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the constant value at the noise maps borders.
        /// </summary>
        public float Border
        {
            get { return _borderValue; }
            set { _borderValue = value; }
        }

        /// <summary>
        /// Gets or sets the generator module.
        /// </summary>
        public SerializableModuleBase Generator
        {
            get { return _generator; }
            set { _generator = value; }
        }

        /// <summary>
        /// Gets the height of the noise map.
        /// </summary>
        public int Height
        {
            get { return _height; }
        }

        /// <summary>
        /// Gets the width of the noise map.
        /// </summary>
        public int Width
        {
            get { return _width; }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets normalized noise map data with all values in the set of {0..1}.
        /// </summary>
        /// <param name="isCropped">Indicates whether to select the cropped (default) or uncropped noise map data.</param>
        /// <param name="xCrop">This value crops off data from the right of the noise map data.</param>
        /// <param name="yCrop">This value crops off data from the bottom of the noise map data.</param>
        /// <returns>The normalized noise map data.</returns>
        public float[,] GetNormalizedData(bool isCropped = true, int xCrop = 0, int yCrop = 0)
        {
            return GetData(isCropped, xCrop, yCrop, true);
        }

        /// <summary>
        /// Gets noise map data.
        /// </summary>
        /// <param name="isCropped">Indicates whether to select the cropped (default) or uncropped noise map data.</param>
        /// <param name="xCrop">This value crops off data from the right of the noise map data.</param>
        /// <param name="yCrop">This value crops off data from the bottom of the noise map data.</param>
        /// <param name="isNormalized">Indicates whether to normalize noise map data.</param>
        /// <returns>The noise map data.</returns>
        public float[,] GetData(bool isCropped = true, int xCrop = 0, int yCrop = 0, bool isNormalized = false)
        {
            int width, height;
            float[,] data;
            if (isCropped)
            {
                width = _width;
                height = _height;
                data = _data;
            }
            else
            {
                width = _ucWidth;
                height = _ucHeight;
                data = _ucData;
            }
            width -= xCrop;
            height -= yCrop;
            var result = new float[width, height];
            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    float sample;
                    if (isNormalized)
                    {
                        sample = (data[x, y] + 1) / 2;
                    }
                    else
                    {
                        sample = data[x, y];
                    }
                    result[x, y] = sample;

                    Debug.Log(sample);
                }
            }
            return result;
        }

        /// <summary>
        /// Clears the noise map.
        /// </summary>
        /// <param name="value">The constant value to clear the noise map with.</param>
        public void Clear(float value = 0f)
        {
            for (var x = 0; x < _width; x++)
            {
                for (var y = 0; y < _height; y++)
                {
                    _data[x, y] = value;
                }
            }
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
        public void GeneratePlanar(double left, double right, double top, double bottom, bool isSeamless = true)
        {
            if (right <= left || bottom <= top)
            {
                throw new ArgumentException("Invalid right/left or bottom/top combination");
            }
            if (_generator == null)
            {
                throw new ArgumentNullException("Generator is null");
            }
            if (!useGPU)
            {
                GeneratePlanarCPU(left, right, top, bottom, isSeamless);
            }
            else
            {
                GeneratePlanarGPU(left, right, top, bottom, isSeamless);
            }
        }

        private void GeneratePlanarGPU(double left, double right, double top, double bottom, bool isSeamless = true)
        {
            // set texture here
            throw new NotImplementedException();
        }

        private void GeneratePlanarCPU(double left, double right, double top, double bottom, bool isSeamless = true)
        {
            var xe = right - left;
            var ze = bottom - top;
            var xd = xe / ((double)_width - _ucBorder);
            var zd = ze / ((double)_height - _ucBorder);
            var xc = left;
            for (var x = 0; x < _ucWidth; x++)
            {
                var zc = top;
                for (var y = 0; y < _ucHeight; y++)
                {
                    float fv;
                    if (!isSeamless)
                    {
                        fv = (float)GeneratePlanar(xc, zc);
                    }
                    else
                    {
                        var swv = GeneratePlanar(xc, zc);
                        var sev = GeneratePlanar(xc + xe, zc);
                        var nwv = GeneratePlanar(xc, zc + ze);
                        var nev = GeneratePlanar(xc + xe, zc + ze);
                        var xb = 1.0 - ((xc - left) / xe);
                        var zb = 1.0 - ((zc - top) / ze);
                        var z0 = Utils.InterpolateLinear(swv, sev, xb);
                        var z1 = Utils.InterpolateLinear(nwv, nev, xb);
                        fv = (float)Utils.InterpolateLinear(z0, z1, zb);
                    }
                    _ucData[x, y] = fv;
                    if (x >= _ucBorder && y >= _ucBorder && x < _width + _ucBorder &&
                        y < _height + _ucBorder)
                    {
                        _data[x - _ucBorder, y - _ucBorder] = fv; // Cropped data
                    }
                    zc += zd;
                }
                xc += xd;
            }
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
            if (angleMax <= angleMin || heightMax <= heightMin)
            {
                throw new ArgumentException("Invalid angle or height parameters");
            }
            if (_generator == null)
            {
                throw new ArgumentNullException("Generator is null");
            }
            if (!useGPU)
            {
                GenerateCylindricalCPU(angleMin, angleMax, heightMin, heightMax);
            }
            else 
            { 
                GenerateCylindricalGPU(); 
            }
        }

        private void GenerateCylindricalGPU()
        {
            // set texture here
            throw new NotImplementedException();
        }

        private void GenerateCylindricalCPU(double angleMin, double angleMax, double heightMin, double heightMax)
        {
            var ae = angleMax - angleMin;
            var he = heightMax - heightMin;
            var xd = ae / ((double)_width - _ucBorder);
            var yd = he / ((double)_height - _ucBorder);
            var ca = angleMin;
            for (var x = 0; x < _ucWidth; x++)
            {
                var ch = heightMin;
                for (var y = 0; y < _ucHeight; y++)
                {
                    _ucData[x, y] = (float)GenerateCylindrical(ca, ch);
                    if (x >= _ucBorder && y >= _ucBorder && x < _width + _ucBorder &&
                        y < _height + _ucBorder)
                    {
                        _data[x - _ucBorder, y - _ucBorder] = (float)GenerateCylindrical(ca, ch);
                        // Cropped data
                    }
                    ch += yd;
                }
                ca += xd;
            }
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
            if (east <= west || south <= north)
            {
                throw new ArgumentException("Invalid east/west or north/south combination");
            }
            if (_generator == null)
            {
                throw new ArgumentNullException("Generator is null");
            }
            if (!useGPU)
            {
                GenerateSphericalCPU(south, north, west, east);
            }
            else
            {
                GenerateSphericalGPU(south, north, west, east);
            }
        }


        private void GenerateSphericalGPU(double south, double north, double west, double east)
        {
            Debug.Log("GenerateSphericalGPU");

            GPURenderingDatas datas = new GPURenderingDatas(new Vector2(Width, Height), ProjectionType.Spherical, RenderingAreaData.standardSpherical);

            //datas.origin = origin;
            // set texture here
            renderedTexture = _generator.GetValueGPU(datas);
        }

        private void GenerateSphericalCPU(double south, double north, double west, double east)
        {
            var loe = east - west;
            var lae = north - south;
            var xd = loe / ((double)_width - _ucBorder);
            var yd = lae / ((double)_height - _ucBorder);
            var clo = west;
            for (var x = 0; x < _ucWidth; x++)
            {
                var cla = south;
                for (var y = 0; y < _ucHeight; y++)
                {
                    _ucData[x, y] = (float)GenerateSpherical(cla, clo);
                    if (x >= _ucBorder && y >= _ucBorder && x < _width + _ucBorder &&
                        y < _height + _ucBorder)
                    {
                        _data[x - _ucBorder, y - _ucBorder] = (float)GenerateSpherical(cla, clo);
                        float sample = _data[x - _ucBorder, y - _ucBorder];
                        // Cropped data
                        if (sample < bounds.x)
                        {
                            bounds = new Vector2(sample, bounds.y);
                        }
                        if (sample > bounds.y)
                        {
                            bounds = new Vector2(bounds.x, sample);
                        }
                    }
                    cla += yd;
                }
                clo += xd;
            }

            distance = Math.Abs(bounds.x - bounds.y);
            Debug.Log(bounds);
        }

        /// <summary>
        /// Creates a grayscale texture map for the current content of the noise map.
        /// </summary>
        /// <returns>The created texture map.</returns>
        public Texture2D GetTexture()
        {
            if (useGPU)
            {
                RenderTexture.active = renderedTexture;
                var tex = new Texture2D(renderedTexture.width, renderedTexture.height);
                tex.ReadPixels(new Rect(0, 0, renderedTexture.width, renderedTexture.height), 0, 0);
                tex.Apply();

                return tex;
            }
            else
            {
                return GetTexture(GradientPresets.Grayscale);
            }
        }

        public RenderTexture getTexture()
        {
            RenderTexture.active = renderedTexture;

            return renderedTexture;
        }
        /// <summary>
        /// Creates a texture map for the current content of the noise map.
        /// </summary>
        /// <param name="gradient">The gradient to color the texture map with.</param>
        /// <returns>The created texture map.</returns>
        public Texture2D GetTexture(Gradient gradient)
        {
            var texture = new Texture2D(_width, _height);
            var pixels = new UnityEngine.Color[_width * _height];
            for (var x = 0; x < _width; x++)
            {
                for (var y = 0; y < _height; y++)
                {
                    float sample;
                    if (!float.IsNaN(_borderValue) &&
                        (x == 0 || x == _width - _ucBorder || y == 0 || y == _height - _ucBorder))
                    {
                        sample = _borderValue;
                    }
                    else
                    {
                        sample = _data[x, y];
                    }
                    pixels[x + y * _width] = gradient.Evaluate((sample + (distance / 2f)) / distance);
                    //Debug.Log((sample + (distance / 2f)) / distance);
                }
            }
            texture.SetPixels(pixels);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.Apply();
            return texture;
        }

        /// <summary>
        /// Creates a normal map for the current content of the noise map.
        /// </summary>
        /// <param name="intensity">The scaling of the normal map values.</param>
        /// <returns>The created normal map.</returns>
        public Texture2D GetNormalMap(float intensity)
        {
            var texture = new Texture2D(_width, _height);
            var pixels = new UnityEngine.Color[_width * _height];
            for (var x = 0; x < _ucWidth; x++)
            {
                for (var y = 0; y < _ucHeight; y++)
                {
                    var xPos = (_ucData[Mathf.Max(0, x - _ucBorder), y] -
                                _ucData[Mathf.Min(x + _ucBorder, _width + _ucBorder), y]) / 2;
                    var yPos = (_ucData[x, Mathf.Max(0, y - _ucBorder)] -
                                _ucData[x, Mathf.Min(y + _ucBorder, _height + _ucBorder)]) / 2;
                    var normalX = new Vector3(xPos * intensity, 0, 1);
                    var normalY = new Vector3(0, yPos * intensity, 1);
                    // Get normal vector
                    var normalVector = normalX + normalY;
                    normalVector.Normalize();
                    // Get color vector
                    var colorVector = Vector3.zero;
                    colorVector.x = (normalVector.x + 1) / 2;
                    colorVector.y = (normalVector.y + 1) / 2;
                    colorVector.z = (normalVector.z + 1) / 2;
                    // Start at (x + _ucBorder, y + _ucBorder) so that resulting normal map aligns with cropped data
                    if (x >= _ucBorder && y >= _ucBorder && x < _width + _ucBorder &&
                        y < _height + _ucBorder)
                    {
                        pixels[(x - _ucBorder) + (y - _ucBorder) * _width] = new UnityEngine.Color(colorVector.x,
                            colorVector.y, colorVector.z);
                    }
                }
            }
            texture.SetPixels(pixels);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.Apply();
            return texture;
        }

        #endregion

        #region IDisposable Members

        [XmlIgnore]
#if !XBOX360 && !ZUNE
        [NonSerialized]
#endif
            private bool _disposed;

        /// <summary>
        /// Gets a value whether the object is disposed.
        /// </summary>
        public bool IsDisposed
        {
            get { return _disposed; }
        }

        /// <summary>
        /// Immediately releases the unmanaged resources used by this object.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = Disposing();
            }
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Immediately releases the unmanaged resources used by this object.
        /// </summary>
        /// <returns>True if the object is completely disposed.</returns>
        protected virtual bool Disposing()
        {
            _data = null;
            _width = 0;
            _height = 0;
            return true;
        }

        #endregion
    }
}
