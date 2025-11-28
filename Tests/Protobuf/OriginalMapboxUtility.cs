using MvtMesherCore.Models;
using Commands = MvtMesherCore.Mapbox.Geometry.CanvasCommand;
using GeomType = MvtMesherCore.Mapbox.Geometry.GeometryType;

public static class OriginalMapboxUtility
{
    public static List<List<MvtUnscaledJsonPoint>> GetGeometry(
			byte geomType, List<uint> geometryCommands
    )
    {
        List<List<MvtUnscaledJsonPoint>> geomOut = new List<List<MvtUnscaledJsonPoint>>();
        List<MvtUnscaledJsonPoint> geomTmp = new List<MvtUnscaledJsonPoint>();
        long cursorX = 0;
        long cursorY = 0;

        int geomCmdCnt = geometryCommands.Count;
        for (int i = 0; i < geomCmdCnt; i++)
        {

            uint g = geometryCommands[i];
            Commands cmd = (Commands)(g & 0x7);
            uint cmdCount = g >> 3;

            if (cmd == Commands.MoveTo || cmd == Commands.LineTo)
            {
                for (int j = 0; j < cmdCount; j++)
                {
                    MvtUnscaledJsonPoint delta = zigzagDecode(geometryCommands[i + 1], geometryCommands[i + 2]);
                    cursorX += delta.X;
                    cursorY += delta.Y;
                    i += 2;
                    //end of part of multipart feature
                    if (cmd == Commands.MoveTo && geomTmp.Count > 0)
                    {
                        geomOut.Add(geomTmp);
                        geomTmp = new List<MvtUnscaledJsonPoint>();
                    }

                    //Point2d pntTmp = new Point2d(cursorX, cursorY);
                    MvtUnscaledJsonPoint pntTmp = new MvtUnscaledJsonPoint()
                    {
                        X = cursorX,
                        Y = cursorY
                    };
                    geomTmp.Add(pntTmp);
                }
            }
            if (cmd == Commands.ClosePath)
            {
                if (geomType == (byte)GeomType.Polygon && geomTmp.Count > 0)
                {
                    geomTmp.Add(geomTmp[0]);
                }
            }
        }

        if (geomTmp.Count > 0)
        {
            geomOut.Add(geomTmp);
        }

        return geomOut;
    }


    private static MvtUnscaledJsonPoint zigzagDecode(long x, long y)
    {

        //TODO: verify speed improvements using
        // new Point2d(){X=x, Y=y} instead of
        // new Point3d(x, y);

        //return new Point2d(
        //    ((x >> 1) ^ (-(x & 1))),
        //    ((y >> 1) ^ (-(y & 1)))
        //);
        return new MvtUnscaledJsonPoint()
        {
            X = ((x >> 1) ^ (-(x & 1))),
            Y = ((y >> 1) ^ (-(y & 1)))
        };
    }
}