﻿/*
    Copyright 2015 MCGalaxy
    
    Dual-licensed under the Educational Community License, Version 2.0 and
    the GNU General Public License, Version 3 (the "Licenses"); you may
    not use this file except in compliance with the Licenses. You may
    obtain a copy of the Licenses at
    
    https://opensource.org/license/ecl-2-0/
    https://www.gnu.org/licenses/gpl-3.0.html
    
    Unless required by applicable law or agreed to in writing,
    software distributed under the Licenses are distributed on an "AS IS"
    BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
    or implied. See the Licenses for the specific language governing
    permissions and limitations under the Licenses.
 */
using MAX.Maths;
using System.Threading;

namespace MAX
{
    public class Entity
    {
        // Raw orientation/position - access must be threadsafe
        public volatile uint _rot;
        public long _pos;

        // Last sent orientation/position, for delta calculation
        public Orientation _lastRot;
        public Position _lastPos;
        public bool hasExtPositions;

        public string Model = "humanoid";
        public AABB ModelBB;
        public string SkinName;
        public float ScaleX, ScaleY, ScaleZ;

        public Orientation Rot
        {
            get { return Orientation.Unpack(_rot); }
            set { _rot = value.Pack(); OnSetRot(); }
        }

        public Position Pos
        {
            get { return Position.Unpack(Interlocked.Read(ref _pos)); }
            set { Interlocked.Exchange(ref _pos, value.Pack()); OnSetPos(); }
        }

        public void SetInitialPos(Position pos)
        {
            Pos = pos; _lastPos = pos;
        }

        public void SetYawPitch(byte yaw, byte pitch)
        {
            Orientation rot = Rot;
            rot.RotY = yaw; rot.HeadX = pitch;
            Rot = rot;
        }

        /// <summary> Whether this player can see the given entity as an entity in the level. </summary>
        public virtual bool CanSeeEntity(Entity other)
        {
            return false;
        }
        public virtual byte EntityID { get; }
        /// <summary> The level this entity is currently on. </summary>
        public virtual Level Level { get; }
        /// <summary> Whether maximum model scale is limited. </summary>
        public virtual bool RestrictsScale { get; }

        public virtual void OnSetPos() { }

        public virtual void OnSetRot() { }


        /// <summary> Sets new model and updates internal state. </summary>  
        public void SetModel(string model)
        {
            Model = model;
            ModelBB = ModelInfo.CalcAABB(this);
        }

        /// <summary> Calls SetModel, then broadcasts the new model to players. </summary>
        public void UpdateModel(string model)
        {
            SetModel(model);
            Entities.BroadcastModel(this, model);
        }
    }
}