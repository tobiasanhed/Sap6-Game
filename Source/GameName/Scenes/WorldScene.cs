﻿using EngineName;
using EngineName.Components;
using EngineName.Components.Renderable;
using EngineName.Logging;
using EngineName.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GameName.Scenes
{
    public class WorldScene : Scene
    {
        public override void Init() {

            var mapSystem = new MapSystem();
            AddSystems(
                new FpsCounterSystem(updatesPerSec: 10),
                new RenderingSystem(),
                new CameraSystem(),
                new PhysicsSystem(),
                mapSystem
            );
            base.Init();
            // Camera entity
            int camera = AddEntity();
            float fieldofview = MathHelper.PiOver2;
            float nearplane = 0.1f;
            float farplane = 1000f;

            int player = AddEntity();
            AddComponent(player, new CBody());
            AddComponent(player, new CInput());
            AddComponent(player, new CTransform());
            AddComponent<C3DRenderable>(player, new CImportedModel() { model = Game1.Inst.Content.Load<Model>("Models/DummySphere") });




            AddComponent(camera, new CCamera(){
                Projection = Matrix.CreatePerspectiveFieldOfView(fieldofview, Game1.Inst.GraphicsDevice.Viewport.AspectRatio,nearplane,farplane)
                ,ClipProjection = Matrix.CreatePerspectiveFieldOfView(fieldofview*1.2f, Game1.Inst.GraphicsDevice.Viewport.AspectRatio, nearplane*0.5f, farplane*1.2f)
            });
            AddComponent(camera, new CTransform() { Position = new Vector3(0, 100, 100), Rotation = Matrix.Identity, Scale = Vector3.One });
            // Tree model entity
            /*int id = AddEntity();
            AddComponent<C3DRenderable>(id, new CImportedModel() { model = Game1.Inst.Content.Load<Model>("Models/tree") });
            AddComponent(id, new CTransform() { Position = new Vector3(0, 0, 0), Rotation = Matrix.Identity, Scale = Vector3.One });*/
            // Heightmap entity
            int id = AddEntity();
            AddComponent<C3DRenderable>(id, new CHeightmap() { Image = Game1.Inst.Content.Load<Texture2D>("Textures/HeightMap") });
            AddComponent(id, new CTransform() { Position = new Vector3(-590, -50, -590), Rotation = Matrix.Identity, Scale = Vector3.One });
            // manually start loading all heightmap components, should be moved/automated
            mapSystem.Load();

            Log.Get().Debug("TestScene initialized.");
        }
    }
}
