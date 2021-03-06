//
// This file is part of the game Voxalia, created by FreneticXYZ.
// This code is Copyright (C) 2016 FreneticXYZ under the terms of the MIT license.
// See README.md or LICENSE.txt for contents of the MIT license.
// If these are not available, see https://opensource.org/licenses/MIT
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.Diagnostics;
using System.Runtime;
using System.Runtime.InteropServices;
using Voxalia.Shared;
using Voxalia.Shared.Files;
using Voxalia.Shared.Collision;
using Voxalia.ServerGame.WorldSystem;
using FreneticScript;
using Voxalia.ServerGame.ServerMainSystem;
using BEPUutilities;

namespace Voxalia.ServerGame.OtherSystems
{
    public class MaterialImage
    {
        public Color[,] Colors;
    }

    public class BlockImageManager
    {
        public const int TexWidth = 4;
        public const int TexWidth2 = TexWidth * 2;
        const int BmpSize = TexWidth * Chunk.CHUNK_SIZE;
        const int BmpSize2 = TexWidth2 * Chunk.CHUNK_SIZE;

        public MaterialImage[] MaterialImages;

        public Object OneAtATimePlease = new Object();

        static readonly Color Transp = Color.FromArgb(0, 0, 0, 0);

        public double Timings_General = 0; // TODO: Per-region var?
        public double Timings_A = 0; // TODO: Per-region var?
        public double Timings_B = 0; // TODO: Per-region var?
        public double Timings_C = 0; // TODO: Per-region var?
        public double Timings_D = 0; // TODO: Per-region var?

        public void RenderChunk(WorldSystem.Region tregion, Vector3i chunkCoords, Chunk chunk)
        {
#if TIMINGS
            Stopwatch sw = new Stopwatch();
            sw.Start();
#endif
            if (tregion.TheServer.CVars.g_renderblocks.ValueB)
            {
                RenderChunkInternal(tregion, chunkCoords, chunk);
            }
            if (tregion.TheServer.CVars.n_rendersides.ValueB)
            {
                RenderChunkInternalAngle(tregion, chunkCoords, chunk);
            }
#if TIMINGS
            sw.Stop();
            Timings_General += sw.ElapsedTicks / (double)Stopwatch.Frequency;
#endif
        }

        public byte[] Combine(List<byte[]> originals, bool angle)
        {
            Bitmap bmp = new Bitmap(Chunk.CHUNK_SIZE * (angle ? TexWidth2 : TexWidth), Chunk.CHUNK_SIZE * (angle ? TexWidth2 : TexWidth), PixelFormat.Format32bppArgb);
            using (Graphics graphics = Graphics.FromImage(bmp))
            {
                graphics.Clear(Transp);
                for (int i = 0; i < originals.Count; i++)
                {
                    DataStream ds = new DataStream(originals[i]);
                    Bitmap tbmp = new Bitmap(ds);
                    graphics.DrawImage(tbmp, 0, 0);
                    tbmp.Dispose();
                }
            }
            DataStream temp = new DataStream();
            bmp.Save(temp, ImageFormat.Png);
            return temp.ToArray();
        }

        Color Blend(Color one, Color two)
        {
            byte a2 = (byte)(255 - one.A);
            return Color.FromArgb((byte)Math.Min(one.A + two.A, 255),
                (byte)(one.R * one.A / 255 + two.R * a2 / 255),
                (byte)(one.G * one.A / 255 + two.G * a2 / 255),
                (byte)(one.B * one.A / 255 + two.B * a2 / 255));
        }

        Color Multiply(Color one, Color two)
        {
            return Color.FromArgb((byte)(one.A * two.A / 255),
                (byte)(one.R * two.R / 255),
                (byte)(one.G * two.G / 255),
                (byte)(one.B * two.B / 255));
        }

        void DrawImage(MaterialImage bmp, MaterialImage bmpnew, int xmin, int ymin, Color col)
        {
            for (int x = 0; x < TexWidth; x++)
            {
                for (int y = 0; y < TexWidth; y++)
                {
                    Color basepx = bmp.Colors[xmin + x, ymin + y];
                    bmp.Colors[xmin + x, ymin + y] = Blend(Multiply(bmpnew.Colors[x, y], col), basepx);
                }
            }
        }

        void DrawImageShiftX(MaterialImage bmp, MaterialImage bmpnew, int xmin, int ymin, Color col)
        {
            xmin += TexWidth;
            for (int x = 0; x < TexWidth; x++)
            {
                for (int y = 0; y < TexWidth; y++)
                {
                    int sx = xmin + x;
                    int sy = ymin + y - x;
                    if (sx < 0 || sy < 0 || sx >= BmpSize2 || sy >= BmpSize2)
                    {
                        continue;
                    }
                    Color basepx = bmp.Colors[sx, sy];
                    bmp.Colors[sx, sy] = Blend(Multiply(bmpnew.Colors[x, y], col), basepx);
                }
            }
        }

        void DrawImageShiftY(MaterialImage bmp, MaterialImage bmpnew, int xmin, int ymin, Color col)
        {
            for (int x = 0; x < TexWidth; x++)
            {
                for (int y = 0; y < TexWidth; y++)
                {
                    int sx = xmin + x;
                    int sy = ymin + y;
                    if (sx < 0 || sy < 0 || sx >= BmpSize2 || sy >= BmpSize2)
                    {
                        continue;
                    }
                    Color basepx = bmp.Colors[sx, sy];
                    bmp.Colors[sx, sy] = Blend(Multiply(bmpnew.Colors[x, y], col), basepx);
                }
            }
        }
        
        void DrawImageShiftZ(MaterialImage bmp, MaterialImage bmpnew, int xmin, int ymin, Color col)
        {
            ymin -= TexWidth;
            xmin += TexWidth;
            for (int x = 0; x < TexWidth; x++)
            {
                for (int y = 0; y < TexWidth; y++)
                {
                    int sx = xmin + x - y;
                    int sy = ymin + y;
                    if (sx < 0 || sy < 0 || sx >= BmpSize2 || sy >= BmpSize2)
                    {
                        continue;
                    }
                    Color basepx = bmp.Colors[sx, sy];
                    bmp.Colors[sx, sy] = Blend(Multiply(bmpnew.Colors[x, y], col), basepx);
                }
            }
        }

        const int mid = Chunk.CHUNK_SIZE / 2;

        void RenderBlockIntoAngle(BlockInternal bi, int x, int y, int z, MaterialImage bmp)
        {
            MaterialImage zmatbmpXP = MaterialImages[bi.Material.TextureID(MaterialSide.XP)];
            MaterialImage zmatbmpYP = MaterialImages[bi.Material.TextureID(MaterialSide.YP)];
            MaterialImage zmatbmpZP = MaterialImages[bi.Material.TextureID(MaterialSide.TOP)];
            if (zmatbmpXP == null || zmatbmpYP == null || zmatbmpZP == null)
            {
                return;
            }
            Color zcolor = Colors.ForByte(bi.BlockPaint);
            if (zcolor.A == 0)
            {
                zcolor = Color.White;
            }
            int x1 = x * TexWidth;
            int y1 = y * TexWidth;
            int z1 = z * TexWidth;
            //    int xw = x * TexWidth;
            //    int yw = y * TexWidth;
            // tileWidth/2*x+tileHeight/2*y, tileWidth/2*x+tileHeight/2*y
            //   int xw = TexWidth * x / 2 + TexWidth * y / 2;
            //   int yw = TexWidth * x / 2 + TexWidth * y / 2;
            // tempPt.x = pt.x - pt.y; tempPt.y = (pt.x + pt.y) / 2;
            int xw = x1 - y1;
            int yw = ((x1 + y1) - (z1)) / 2;
            //   tempPt.x = (2 * pt.y + pt.x) / 2; tempPt.y = (2 * pt.y - pt.x) / 2;
            // int xw = (2 * y1 + x1) / 2;
            //  int yw = (2 * y1 - x1) / 2;
            xw += BmpSize2 / 2;
            yw += BmpSize2 / 2;
            DrawImageShiftX(bmp, zmatbmpXP, xw, yw, zcolor);
            DrawImageShiftY(bmp, zmatbmpYP, xw, yw, zcolor);
            DrawImageShiftZ(bmp, zmatbmpZP, xw, yw, zcolor);
        }

        void RenderChunkInternalAngle(WorldSystem.Region tregion, Vector3i chunkCoords, Chunk chunk)
        {
            MaterialImage bmp = new MaterialImage() { Colors = new Color[BmpSize2, BmpSize2] };
            for (int z = 0; z < Chunk.CHUNK_SIZE; z++)
            {
                for (int x = 0; x < Chunk.CHUNK_SIZE; x++)
                {
                    for (int y = 0; y < Chunk.CHUNK_SIZE; y++)
                    {
                        // TODO: async chunk read locker?
                        BlockInternal bi = chunk.GetBlockAt(x, y, z);
                        if (bi.Material.RendersAtAll())
                        {
                            RenderBlockIntoAngle(bi, x, y, z, bmp);
                        }
                    }
                }
            }
            Bitmap tbmp = new Bitmap(BmpSize2, BmpSize2);
            BitmapData bdat = tbmp.LockBits(new Rectangle(0, 0, tbmp.Width, tbmp.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            int stride = bdat.Stride;
            // Surely there's a better way to do this!
            unsafe
            {
                byte* ptr = (byte*)bdat.Scan0;
                for (int x = 0; x < BmpSize2; x++)
                {
                    for (int y = 0; y < BmpSize2; y++)
                    {
                        Color tcol = bmp.Colors[x, y];
                        ptr[(x * 4) + y * stride + 0] = tcol.B;
                        ptr[(x * 4) + y * stride + 1] = tcol.G;
                        ptr[(x * 4) + y * stride + 2] = tcol.R;
                        ptr[(x * 4) + y * stride + 3] = tcol.A;
                    }
                }
            }
            DataStream ds = new DataStream();
            tbmp.Save(ds, ImageFormat.Png);
            tregion.ChunkManager.WriteImageAngle((int)chunkCoords.X, (int)chunkCoords.Y, (int)chunkCoords.Z, ds.ToArray());
        }

        void RenderChunkInternal(WorldSystem.Region tregion, Vector3i chunkCoords, Chunk chunk)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            MaterialImage bmp = new MaterialImage() { Colors = new Color[BmpSize, BmpSize] };
            for (int x = 0; x < Chunk.CHUNK_SIZE; x++)
            {
                for (int y = 0; y < Chunk.CHUNK_SIZE; y++)
                {
                    // TODO: async chunk read locker?
                    BlockInternal topOpaque = BlockInternal.AIR;
                    int topZ = 0;
                    for (int z = 0; z < Chunk.CHUNK_SIZE; z++)
                    {
                        BlockInternal bi = chunk.GetBlockAt(x, y, z);
                        if (bi.IsOpaque())
                        {
                            topOpaque = bi;
                            topZ = z;
                        }
                    }
                    if (!topOpaque.Material.RendersAtAll())
                    {
                        DrawImage(bmp, MaterialImages[0], x * TexWidth, y * TexWidth, Color.Transparent);
                    }
                    for (int z = topZ; z < Chunk.CHUNK_SIZE; z++)
                    {
                        BlockInternal bi = chunk.GetBlockAt(x, y, z);
                        if (bi.Material.RendersAtAll())
                        {
                            MaterialImage zmatbmp = MaterialImages[bi.Material.TextureID(MaterialSide.TOP)];
                            if (zmatbmp == null)
                            {
                                continue;
                            }
                            Color zcolor = Colors.ForByte(bi.BlockPaint);
                            if (zcolor.A == 0)
                            {
                                zcolor = Color.White;
                            }
                            DrawImage(bmp, zmatbmp, x * TexWidth, y * TexWidth, zcolor);
                        }
                    }
                }
            }
            sw.Stop();
            Timings_A += sw.ElapsedTicks / (double)Stopwatch.Frequency;
            sw.Reset();
            sw.Start();
            Bitmap tbmp = new Bitmap(BmpSize2, BmpSize2);
            BitmapData bdat = tbmp.LockBits(new Rectangle(0, 0, tbmp.Width, tbmp.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            int stride = bdat.Stride;
            // Surely there's a better way to do this!
            unsafe
            {
                byte* ptr = (byte*)bdat.Scan0;
                for (int x = 0; x < BmpSize; x++)
                {
                    for (int y = 0; y < BmpSize; y++)
                    {
                        Color tcol = bmp.Colors[x, y];
                        ptr[(x * 4) + y * stride + 0] = tcol.B;
                        ptr[(x * 4) + y * stride + 1] = tcol.G;
                        ptr[(x * 4) + y * stride + 2] = tcol.R;
                        ptr[(x * 4) + y * stride + 3] = tcol.A;
                    }
                }
            }
            tbmp.UnlockBits(bdat);
            sw.Stop();
            Timings_B += sw.ElapsedTicks / (double)Stopwatch.Frequency;
            sw.Reset();
            sw.Start();
            DataStream ds = new DataStream();
            tbmp.Save(ds, ImageFormat.Png);
            tbmp.Dispose();
            sw.Stop();
            Timings_C += sw.ElapsedTicks / (double)Stopwatch.Frequency;
            sw.Reset();
            sw.Start();
            lock (OneAtATimePlease) // NOTE: We can probably make this grab off an array of locks to reduce load a little.
            {
                KeyValuePair<int, int> maxes = tregion.ChunkManager.GetMaxes((int)chunkCoords.X, (int)chunkCoords.Y);
                tregion.ChunkManager.SetMaxes((int)chunkCoords.X, (int)chunkCoords.Y, Math.Min(maxes.Key, (int)chunkCoords.Z), Math.Max(maxes.Value, (int)chunkCoords.Z));
            }
            tregion.ChunkManager.WriteImage((int)chunkCoords.X, (int)chunkCoords.Y, (int)chunkCoords.Z, ds.ToArray());
            sw.Stop();
            Timings_D += sw.ElapsedTicks / (double)Stopwatch.Frequency;
        }

        public void Init(Server tserver)
        {
            // TODO: v0.1.0 texture config update!
            MaterialImages = new MaterialImage[MaterialHelpers.Textures.Length];
            for (int i = 0; i < MaterialImages.Length; i++)
            {
                string tex = MaterialHelpers.Textures[i];
                string actualtexture = "textures/" + tex.Before(",").Before("&").Before("$").Before("@")+ ".png";
                try
                {
                    Bitmap bmp1 = new Bitmap(tserver.Files.ReadToStream(actualtexture));
                    Bitmap bmp2 = new Bitmap(bmp1, new Size(TexWidth, TexWidth));
                    bmp1.Dispose();
                    MaterialImage img = new MaterialImage();
                    img.Colors = new Color[TexWidth, TexWidth];
                    for (int x = 0; x < TexWidth; x++)
                    {
                        for (int y = 0; y < TexWidth; y++)
                        {
                            img.Colors[x, y] = bmp2.GetPixel(x, y);
                        }
                    }
                    MaterialImages[i] = img;
                    bmp2.Dispose();
                }
                catch (Exception ex)
                {
                    Utilities.CheckException(ex);
                    SysConsole.Output("loading texture for " + i + ": '" + actualtexture + "'", ex);
                }
            }
            SysConsole.Output(OutputType.INIT, "Loaded " + MaterialImages.Length + " textures!");
        }
    }
}
