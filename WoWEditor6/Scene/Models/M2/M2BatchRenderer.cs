﻿using System;
using System.Linq;
using System.Runtime.InteropServices;
using SharpDX;
using WoWEditor6.Graphics;
using WoWEditor6.IO.Files.Models;

namespace WoWEditor6.Scene.Models.M2
{
    class M2BatchRenderer : IDisposable
    {
        [StructLayout(LayoutKind.Sequential)]
        struct PerInstanceBuffer
        {
            public Matrix matInstance;
            public Color4 colorMod;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct PerModelPassBuffer
        {
            public Matrix uvAnimMatrix;
            public Vector4 modelPassParams;
        }

        private static Mesh gMesh;
        private static Sampler gSampler;

        private static readonly BlendState[] BlendStates = new BlendState[2];

        private static ShaderProgram gMaskBlendProgram;
        private static ShaderProgram gNoBlendProgram;
        private static RasterState gNoCullState;
        private static RasterState gCullState;

        public M2File Model { get; private set; }

        private VertexBuffer mInstanceBuffer;

        private int mInstanceCount;

        private PerInstanceBuffer[] mActiveInstances = new PerInstanceBuffer[0];
        private static ConstantBuffer gPerPassBuffer;

        public M2BatchRenderer(M2File model)
        {
            Model = model;
        }

        public virtual void Dispose()
        {
            var ib = mInstanceBuffer;
            WorldFrame.Instance.Dispatcher.BeginInvoke(() =>
            {
                if (ib != null)
                    ib.Dispose();
            });
        }

        public static void BeginDraw()
        {
            gMesh.BeginDraw();
            gMesh.Program.SetPixelSampler(0, gSampler);
            gMesh.Program.SetVertexConstantBuffer(2, gPerPassBuffer);
        }

        public void OnFrame(M2Renderer renderer)
        {
            UpdateVisibleInstances(renderer);
            if (mInstanceCount == 0)
                return;

            gMesh.UpdateIndexBuffer(renderer.IndexBuffer);
            gMesh.UpdateVertexBuffer(renderer.VertexBuffer);
            gMesh.UpdateInstanceBuffer(mInstanceBuffer);
            gMesh.Program.SetVertexConstantBuffer(1, renderer.AnimBuffer);

            foreach (var pass in Model.Passes)
            {
                // This renderer is only for opaque pass
                if (pass.BlendMode != 0 && pass.BlendMode != 1)
                    continue;

                var program = pass.BlendMode == 0 ? gNoBlendProgram : gMaskBlendProgram;
                if (program != gMesh.Program)
                {
                    gMesh.Program = program;
                    program.Bind();
                }

                var cullingDisabled = (pass.RenderFlag & 0x04) != 0;
                gMesh.UpdateRasterizerState(cullingDisabled ? gNoCullState : gCullState);
                gMesh.UpdateBlendState(BlendStates[pass.BlendMode]);

                var unlit = ((pass.RenderFlag & 0x01) != 0) ? 0.0f : 1.0f;
                var unfogged = ((pass.RenderFlag & 0x02) != 0) ? 0.0f : 1.0f;

                Matrix uvAnimMat;
                renderer.Animator.GetUvAnimMatrix(pass.TexAnimIndex, out uvAnimMat);

                gPerPassBuffer.UpdateData(new PerModelPassBuffer
                {
                    uvAnimMatrix = uvAnimMat,
                    modelPassParams = new Vector4(unlit, unfogged, 0.0f, 0.0f)
                });

                gMesh.StartVertex = 0;
                gMesh.StartIndex = pass.StartIndex;
                gMesh.IndexCount = pass.IndexCount;
                gMesh.Program.SetPixelTexture(0, pass.Textures.First());
                gMesh.Draw(mInstanceCount);
            }
        }

        private void UpdateVisibleInstances(M2Renderer renderer)
        {
            lock (renderer.VisibleInstances)
            {
                if (mActiveInstances.Length < renderer.VisibleInstances.Count)
                    mActiveInstances = new PerInstanceBuffer[renderer.VisibleInstances.Count];

                for (var i = 0; i < renderer.VisibleInstances.Count; ++i)
                {
                    mActiveInstances[i].matInstance = renderer.VisibleInstances[i].InstanceMatrix;
                    mActiveInstances[i].colorMod = renderer.VisibleInstances[i].HighlightColor;
                }

                mInstanceCount = renderer.VisibleInstances.Count;
                if (mInstanceCount == 0)
                    return;
            }

            mInstanceBuffer.UpdateData(mActiveInstances);
        }

        public void OnSyncLoad()
        {
            var ctx = WorldFrame.Instance.GraphicsContext;
            mInstanceBuffer = new VertexBuffer(ctx);
        }

        public static void Initialize(GxContext context)
        {
            gMesh = new Mesh(context)
            {
                Stride = IO.SizeCache<M2Vertex>.Size,
                InstanceStride = IO.SizeCache<PerInstanceBuffer>.Size,
                DepthState = {
                    DepthEnabled = true,
                    DepthWriteEnabled = true
                }
            };

            gMesh.BlendState.Dispose();
            gMesh.IndexBuffer.Dispose();
            gMesh.VertexBuffer.Dispose();

            gMesh.AddElement("POSITION", 0, 3);
            gMesh.AddElement("BLENDWEIGHT", 0, 4, DataType.Byte, true);
            gMesh.AddElement("BLENDINDEX", 0, 4, DataType.Byte);
            gMesh.AddElement("NORMAL", 0, 3);
            gMesh.AddElement("TEXCOORD", 0, 2);
            gMesh.AddElement("TEXCOORD", 1, 2);

            gMesh.AddElement("TEXCOORD", 2, 4, DataType.Float, false, 1, true);
            gMesh.AddElement("TEXCOORD", 3, 4, DataType.Float, false, 1, true);
            gMesh.AddElement("TEXCOORD", 4, 4, DataType.Float, false, 1, true);
            gMesh.AddElement("TEXCOORD", 5, 4, DataType.Float, false, 1, true);
            gMesh.AddElement("COLOR", 0, 4, DataType.Float, false, 1, true);

            gNoBlendProgram = new ShaderProgram(context);
            gNoBlendProgram.SetVertexShader(Resources.Shaders.M2VertexInstanced);
            gNoBlendProgram.SetPixelShader(Resources.Shaders.M2Pixel);

            gMaskBlendProgram = new ShaderProgram(context);
            gMaskBlendProgram.SetVertexShader(Resources.Shaders.M2VertexInstanced);
            gMaskBlendProgram.SetPixelShader(Resources.Shaders.M2PixelBlendAlpha);

            gPerPassBuffer = new ConstantBuffer(context);
            gPerPassBuffer.UpdateData(new PerModelPassBuffer()
            {
                uvAnimMatrix = Matrix.Identity,
                modelPassParams = Vector4.Zero
            });

            gMesh.Program = gNoBlendProgram;

            gSampler = new Sampler(context)
            {
                AddressMode = SharpDX.Direct3D11.TextureAddressMode.Wrap,
                Filter = SharpDX.Direct3D11.Filter.MinMagMipLinear
            };

            for (var i = 0; i < BlendStates.Length; ++i)
                BlendStates[i] = new BlendState(context);

            BlendStates[0] = new BlendState(context)
            {
                BlendEnabled = false
            };

            BlendStates[1] = new BlendState(context)
            {
                BlendEnabled = true,
                SourceBlend = SharpDX.Direct3D11.BlendOption.One,
                DestinationBlend = SharpDX.Direct3D11.BlendOption.Zero,
                SourceAlphaBlend = SharpDX.Direct3D11.BlendOption.One,
                DestinationAlphaBlend = SharpDX.Direct3D11.BlendOption.Zero
            };

            gNoCullState = new RasterState(context) { CullEnabled = false };
            gCullState = new RasterState(context) { CullEnabled = true };
        }
    }
}
