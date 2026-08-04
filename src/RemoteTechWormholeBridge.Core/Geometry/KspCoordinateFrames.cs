namespace RemoteTechWormholeBridge.Core.Geometry
{
    public static class KspCoordinateFrames
    {
        public static Vector3Value OrbitalToWorld(Vector3Value orbital)
        {
            return new Vector3Value(orbital.X, orbital.Z, orbital.Y);
        }
    }
}
