namespace GL2O;

public class Texture2D : GLObject
{
    public int Id { get; private set; }

    public int Width { get; }
    public int Height { get; }

    public Texture2D(int width, int height, int levels = 1, SizedInternalFormat fmt = SizedInternalFormat.Rgba8)
    {
        GL.CreateTextures(TextureTarget.Texture2D, 1, out int id);
        Id = id;
        Width = width;
        Height = height;

        GL.TextureStorage2D(id, levels, fmt, width, height);

        GL.TextureParameter(id, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
        GL.TextureParameter(id, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

        GL.TextureParameter(id, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
        GL.TextureParameter(id, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
    }

    public unsafe void SetPixels<T>(ReadOnlySpan<T> data, int offsetX, int offsetY, int width, int height, PixelFormat fmt, PixelType type, int rowLength = 0) where T : unmanaged
    {
        if (rowLength > 0) GL.PixelStore(PixelStoreParameter.UnpackRowLength, rowLength);

        GL.TextureSubImage2D(Id, 0, offsetX, offsetY, width, height, fmt, type, ref MemoryMarshal.GetReference(data));
        //GL.GenerateTextureMipmap(Id);

        if (rowLength > 0) GL.PixelStore(PixelStoreParameter.UnpackRowLength, 0);
    }

    public void BindUnit(int unitId)
    {
        GL.BindTextureUnit(unitId, Id);
    }

    public void Dispose()
    {
        if (Id != 0) {
            GL.DeleteTexture(Id);
            Id = 0;
        }
    }
}