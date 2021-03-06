/*
Copyright (C) 2010 Zone Five Software

This library is free software; you can redistribute it and/or
modify it under the terms of the GNU Lesser General Public
License as published by the Free Software Foundation; either
version 2 of the License, or (at your option) any later version.

This library is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public
License along with this library. If not, see <http://www.gnu.org/licenses/>.
 */
// Author: Aaron Averill


using System;
using System.Collections.Generic;
using System.Text;

using ZoneFiveSoftware.Common.Visuals.Fitness;

namespace ZoneFiveSoftware.SportTracks.Device.Globalsat
{
    class ExtendFitnessDevices : IExtendFitnessDevices
    {
        public IList<IFitnessDevice> FitnessDevices
        {
            //Depreciate use of GH615, though possibly handled in generic import
            get { return new IFitnessDevice[] { 
#if GLOBALSAT_DEVICE
                new FitnessDevice_GsSport(), new FitnessDevice_GH625(), new FitnessDevice_GH505(), 
                new FitnessDevice_GH625XT(), new FitnessDevice_GB580(), new FitnessDevice_GB1000(), new FitnessDevice_GH561(),
#elif A_RIVAL_DEVICE
                //Debug builds could contain Spoq too, but then there will be errors in the log if GlobalSat-debug and Spoq is installed at the same time
                new FitnessDevice_SpoQ()
#endif
            };
            }
        }
    }
}
