using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Common.OpenGL
{

    /// <summary>
    /// trimmmed down wrapper for GLSL shaders with compilation, linkage, uniforms binding, etc. 
    /// </summary>
    public class Effect : GraphicsResource
    {
        #region fields and properties

        #region OpenGL handles

        /// <summary>
        /// GL vertex shader handle
        /// </summary>
        private int _vertexShader;

        /// <summary>
        /// GL fragment shader handle
        /// </summary>
        private int _fragmentShader;

        /// <summary>
        /// GL shader program (vert + frag) handle
        /// </summary>
        private int _shaderProgram;




        #endregion

        #region shader uniforms

        public Dictionary<string, int> Uniforms { get; private set; }
        public Matrix4 WorldMatrix { get; set; }
        public Matrix4 ViewMatrix { get; set; } 
        public Matrix4 ProjectionMatrix { get; set; }

        /// <summary>
        /// OpenGL ID for a texture
        /// </summary>
        public int Texture { get; set; } 

        #endregion

        #endregion

        #region initialization

        /// <summary>
        /// basic OpenTK wrapper for vertex + fragment shader 
        /// </summary>
        /// <param name="vertexShaderPath"></param>
        /// <param name="fragmentShaderPath"></param>
        public Effect(string vertexShaderPath, string fragmentShaderPath)
        {
			vertexShaderPath = vertexShaderPath.Replace ('\\', Path.DirectorySeparatorChar);
			fragmentShaderPath = fragmentShaderPath.Replace ('\\', Path.DirectorySeparatorChar);

            var vertexSource = File.ReadAllText(vertexShaderPath);
            var fragmentSource = File.ReadAllText(fragmentShaderPath);

            if (!BuildProgram(vertexSource, fragmentSource, out _vertexShader, out _fragmentShader, out _shaderProgram))
            {
                throw new Exception("An error occurred while compiling a shader program.");
            }

            WorldMatrix = Matrix4.Identity;
            ViewMatrix = Matrix4.Identity;
            ProjectionMatrix = Matrix4.Identity;
        }

        #endregion

        #region rendering


        /// <summary>
        /// use this program, and set any variables that are required
        /// </summary>
        public override void Bind()
        {
            GL.UseProgram(_shaderProgram);
            SetMatrix("WorldMatrix", WorldMatrix);
            SetMatrix("ViewMatrix", ViewMatrix);
            SetMatrix("ProjectionMatrix", ProjectionMatrix);
            SetTexture("Texture", Texture);
        }

        #endregion

        #region compilation

        /// <summary>
        /// compile and link a program from the given shaders.
        /// </summary>
        /// <param name="vertexShaderSource">GLSL source for the vertex shader</param>
        /// <param name="fragmentShaderSource">GLSL source for the fragment shader</param>
        /// <returns>Success status</returns>
        /// <remarks>
        /// TODO: extend this to support other shader types - hull, geometry, compute, etc.
        /// </remarks>
        public bool BuildProgram(string vertexShaderSource, string fragmentShaderSource, out int vs, out int fs, out int program)
        {
            var hasErrors = false;

            #region compile the vertex shader

            vs = GL.CreateShader(ShaderType.VertexShader);

            GL.ShaderSource(vs, vertexShaderSource);
            GL.CompileShader(vs);

            hasErrors = PrintShaderInfoLog(vs);

            #endregion

            #region compile the fragment shader

            fs = GL.CreateShader(ShaderType.FragmentShader);

            GL.ShaderSource(fs, fragmentShaderSource);
            GL.CompileShader(fs);

            hasErrors |= PrintShaderInfoLog(fs);

            #endregion

            #region build the program and bind vertex attributes

            program = GL.CreateProgram();

            GL.AttachShader(program, vs);
            GL.AttachShader(program, fs);

            /// link attributes based on the vertex definition

            BindAttributes(program);

            GL.LinkProgram(program);

            Uniforms = LoadUniformsList(program);

            hasErrors |= PrintProgramInfoLog(program);

            #endregion

            return !hasErrors;

        }

        /// <summary>
        /// bind vertex attributes (only used if you're sending, say, a position as well as texture coordinates, etc - not used in this demo
        /// </summary>
        /// <param name="program"></param>
        private void BindAttributes(int program)
        {
        }

        #endregion

        #region parameter mapping

        /// <summary>
        /// load the list of uniforms for the specified shader program
        /// </summary>
        /// <param name="program"></param>
        public static Dictionary<string, int> LoadUniformsList(int program)
        {
            var uniformLocations = new Dictionary<string, int>();

            var numActiveUniforms = 0;
            GL.GetProgram(program, ProgramParameter.ActiveUniforms, out numActiveUniforms);

            for (var i = 0; i < numActiveUniforms; i++)
            {
                var uniformName = GL.GetActiveUniformName(program, i);
                uniformLocations[uniformName] = i;
            }

            return uniformLocations;
        }

        /// <summary>
        /// assign a value to a uniform/global shader variable
        /// </summary>
        /// <remarks>
        /// Add overrides for different parameter types
        /// </remarks>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public void SetMatrix(string name, Matrix4 value)
        {
            var location = 0;

            if (Uniforms.TryGetValue(name, out location))
                GL.UniformMatrix4(location, false, ref value);
            else
                Trace.TraceWarning("No uniform named \"{0}\" is available. Check your shader source to ensure that the name is spelled correctly, of the correct case, and was not removed during optimization",name);

        }

        /// <summary>
        /// attach a texture value to the shader program
        /// </summary>
        /// <param name="name"></param>
        /// <param name="texture"></param>
        public void SetTexture(string name, int texture, TextureTarget target = TextureTarget.Texture2D, TextureUnit unit = TextureUnit.Texture0)
        {
            var location = 0;

            if (Uniforms.TryGetValue(name, out location))
            {
                GL.ActiveTexture(unit);
                GL.BindTexture(target, texture);
                GL.Uniform1(location, unit - TextureUnit.Texture0);
            }
            else
                Trace.TraceWarning("No uniform named \"{0}\" is available. Check your shader source to ensure that the name is spelled correctly, of the correct case, and was not removed during optimization",name);

        }

        #endregion

        #region logging

        /// <summary>
        /// retrieve the shader compilation log from GL and print it to the console
        /// </summary>
        /// <param name="shaderHandle"></param>
        /// <returns></returns>
        private static bool PrintShaderInfoLog(int shaderHandle)
        {

            var log = GL.GetShaderInfoLog(shaderHandle);
            var hasErrors = false;

            if (!string.IsNullOrWhiteSpace(log))
            {
                hasErrors = true;
                Trace.TraceError("\nShader log: \"{0}\"\n", log);
            }

            return hasErrors;
        }

        /// <summary>
        /// retrieve the program link log from GL and print it to the console
        /// </summary>
        /// <param name="programHandle"></param>
        /// <returns></returns>
        private static bool PrintProgramInfoLog(int programHandle)
        {
            var hasErrors = false;
            var log = GL.GetProgramInfoLog(programHandle);

            if (!string.IsNullOrWhiteSpace(log))
            {
                Trace.TraceError("Program log: \"{0}\"\n", log);
                hasErrors = true;
            }

            return hasErrors;

        }


        #endregion

        #region cleanup

        /// <summary>
        /// clean up any GL resources
        /// </summary>
        public override void Dispose()
        {
            if (GL.IsShader(_vertexShader))
            {
                GL.DeleteShader(_vertexShader);
                _vertexShader = -1;
            }

            if (GL.IsShader(_fragmentShader))
            {
                GL.DeleteShader(_fragmentShader);
                _fragmentShader = -1;
            }

            if (GL.IsProgram(_shaderProgram))
            {
                GL.UseProgram(0);
                GL.DeleteProgram(_shaderProgram);
                _shaderProgram = 0;
            }

            base.Dispose();
        }


        #endregion
    }
}
