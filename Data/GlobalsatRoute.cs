/*
 *  Globalsat/Keymaze SportTracks Plugin
 *  Copyright 2009 John Philip 
 * 
 *  This software may be used and distributed according to the terms of the
 *  GNU Lesser General Public License version 2 or any later version.
 */

using System;
using System.Collections.Generic;
using System.Text;

namespace ZoneFiveSoftware.SportTracks.Device.Globalsat
{
    public class GlobalsatRoute
    {
        public const int MaxRouteNameLength = 15;

        public string Name;
        public IList<GlobalsatWaypoint> wpts;

        public GlobalsatRoute()
        {
            this.Name = "";
            this.wpts = new List<GlobalsatWaypoint>();
        }

        public GlobalsatRoute(string name, IList<GlobalsatWaypoint> wpts)
        {
            this.Name = name;
            this.wpts = wpts;
        }
    }
}
