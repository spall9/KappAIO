using EloBuddy;
using EloBuddy.SDK;
using SharpDX;

namespace KappAIO.Champions.Sion
{
    internal static class Stuff
    {
        internal static Geometry.Polygon.Rectangle QRectangle(Vector3 pos)
        { return new Geometry.Polygon.Rectangle(Player.Instance.ServerPosition, Player.Instance.ServerPosition.Extend(pos, Sion.Q.Range).To3D(), Sion.Q.Width); }

        internal static Geometry.Polygon.Rectangle QRectangle(this Obj_AI_Base target) { return QRectangle(target.ServerPosition); }

        internal static float MaxERange = 1375;

        internal static Vector3 EndPos(Vector3 pos)
        {
            return Player.Instance.ServerPosition.Extend(pos, MaxERange).To3DWorld();
        }

        internal static Vector3 EndPos(this Obj_AI_Base target)
        {
            return EndPos(target.ServerPosition);
        }

        internal static Geometry.Polygon.Rectangle ERectangle(Obj_AI_Base target)
        {
            return new Geometry.Polygon.Rectangle(target.ServerPosition, target.EndPos(), target.BoundingRadius);
        }
    }
}
