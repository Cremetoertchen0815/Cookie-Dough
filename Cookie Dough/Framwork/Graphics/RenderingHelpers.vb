Imports Microsoft.Xna.Framework
Imports Microsoft.Xna.Framework.Graphics

Namespace Framework.Graphics
    Public Module RenderingHelpers


        'Apply the default lighting
        Friend Sub ApplyDefaultFX(effect As BasicEffect, Projection As Matrix, Optional yflip As Integer = 1)
            effect.LightingEnabled = True
            effect.AmbientLightColor = Color.White.ToVector3 * 0.06
            effect.DirectionalLight0.Enabled = True
            effect.DirectionalLight0.DiffuseColor = Color.White.ToVector3 * 0.25
            effect.DirectionalLight0.Direction = New Vector3(0.7, yflip, 0.7)
            effect.DirectionalLight0.SpecularColor = Color.SkyBlue.ToVector3 * 0.5
            effect.DirectionalLight1.Enabled = True
            effect.DirectionalLight1.DiffuseColor = Color.White.ToVector3 * 0.25
            effect.DirectionalLight1.Direction = New Vector3(-0.7, yflip, -0.7)
            effect.DirectionalLight1.SpecularColor = Color.SkyBlue.ToVector3 * 0.5
            effect.DirectionalLight2.Enabled = True
            effect.DirectionalLight2.DiffuseColor = Color.White.ToVector3 * 0.35
            effect.DirectionalLight2.SpecularColor = Color.SkyBlue.ToVector3 * 0.1
            effect.SpecularPower = 15
            effect.Alpha = 1
            effect.Projection = Projection
        End Sub
        Friend Sub ApplyDefaultFX(model As Model, Projection As Matrix, Optional yflip As Integer = 1)
            For Each element In model.Meshes
                For Each fx As BasicEffect In element.Effects
                    ApplyDefaultFX(fx, Projection, yflip)
                Next
            Next
        End Sub
    End Module
End Namespace