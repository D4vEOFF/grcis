﻿// CSG scene:
CSGInnerNode root = new CSGInnerNode(SetOperation.Union);
root.SetAttribute(PropertyName.REFLECTANCE_MODEL, new PhongModel());
root.SetAttribute(PropertyName.MATERIAL, new PhongMaterial(new double[] { 0.5, 0.5, 0.5 }, 0.1, 0.6, 0.3, 16));
sc.Intersectable = root;

// Background color:
sc.BackgroundColor = new double[] { 0.0, 0.05, 0.05 };

// Camera:
sc.Camera = new StaticCamera(new Vector3d(0.0, 0.0, -10.0),
                             new Vector3d(0.0, 0.0, 1.0),
                             60.0);

// Light sources:
sc.Sources = new System.Collections.Generic.LinkedList<ILightSource>();
sc.Sources.Add(new AmbientLightSource(0.8));
sc.Sources.Add(new PointLightSource(new Vector3d(-5.0, 3.0, -3.0), 1.0));

// --- NODE DEFINITIONS ----------------------------------------------------

// sphere 1:
Sphere s = new Sphere();
s.SetAttribute(PropertyName.COLOR, new double[] { 1.0, 0.6, 0.0 });
root.InsertChild(s, Matrix4d.Identity);

// sphere 2:
s = new Sphere();
s.SetAttribute(PropertyName.COLOR, new double[] { 0.2, 0.9, 0.5 });
root.InsertChild(s, Matrix4d.CreateTranslation(-2.2, 0.0, 0.0));

// sphere 3:
s = new Sphere();
s.SetAttribute(PropertyName.COLOR, new double[] { 0.1, 0.3, 1.0 });
root.InsertChild(s, Matrix4d.CreateTranslation(-4.4, 0.0, 0.0));

// sphere 4:
s = new Sphere();
s.SetAttribute(PropertyName.COLOR, new double[] { 1.0, 0.2, 0.2 });
root.InsertChild(s, Matrix4d.CreateTranslation(2.2, 0.0, 0.0));

// sphere 5:
s = new Sphere();
s.SetAttribute(PropertyName.COLOR, new double[] { 0.1, 0.4, 0.0 });
s.SetAttribute(PropertyName.TEXTURE, new CheckerTexture(80.0, 40.0, new double[] { 1.0, 0.8, 0.2 }));
root.InsertChild(s, Matrix4d.CreateTranslation(4.4, 0.0, 0.0));
