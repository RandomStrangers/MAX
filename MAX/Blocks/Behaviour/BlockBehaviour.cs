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
using MAX.Blocks.Physics;
namespace MAX.Blocks
{
    public delegate ChangeResult HandleDelete(Player p, ushort oldBlock, ushort x, ushort y, ushort z);
    public delegate ChangeResult HandlePlace(Player p, ushort newBlock, ushort x, ushort y, ushort z);
    public delegate bool HandleWalkthrough(Player p, ushort block, ushort x, ushort y, ushort z);
    public delegate void HandlePhysics(Level lvl, ref PhysInfo C);
    public static class BlockBehaviour
    {
        public static HandlePlace GetPlaceHandler(ushort block, BlockProps[] props)
        {
            switch (block)
            {
                case Block.C4: 
                    return PlaceBehaviour.C4;
                case Block.C4Detonator: 
                    return PlaceBehaviour.C4Det;
            }
            if (props[block].GrassBlock != Block.Invalid)
            {
                return PlaceBehaviour.DirtGrow;
            }
            if (props[block].DirtBlock != Block.Invalid)
            {
                return PlaceBehaviour.GrassDie;
            }
            if (props[block].StackBlock != Block.Air)
            {
                return PlaceBehaviour.Stack;
            }
            return null;
        }
        public static HandleDelete GetDeleteHandler(ushort block, BlockProps[] props)
        {
            switch (block)
            {
                case Block.RocketStart: 
                    return DeleteBehaviour.RocketStart;
                case Block.Fireworks: 
                    return DeleteBehaviour.Firework;
                case Block.C4Detonator: 
                    return DeleteBehaviour.C4Det;
                case Block.Door_Log_air: 
                    return DeleteBehaviour.RevertDoor;
                case Block.Door_TNT_air: 
                    return DeleteBehaviour.RevertDoor;
                case Block.Door_Green_air: 
                    return DeleteBehaviour.RevertDoor;
            }
            if (props[block].IsMessageBlock)
            {
                return DeleteBehaviour.DoMessageBlock;
            }
            if (props[block].IsPortal)
            {
                return DeleteBehaviour.DoPortal;
            }
            if (props[block].IsTDoor)
            {
                return DeleteBehaviour.RevertDoor;
            }
            if (props[block].oDoorBlock != Block.Invalid)
            {
                return DeleteBehaviour.ODoor;
            }
            if (props[block].IsDoor)
            {
                return DeleteBehaviour.Door;
            }
            return null;
        }
        public static HandleWalkthrough GetWalkthroughHandler(ushort block, BlockProps[] props, bool nonSolid)
        {
            switch (block)
            {
                case Block.Checkpoint: 
                    return WalkthroughBehaviour.Checkpoint;
                case Block.Door_AirActivatable: 
                    return WalkthroughBehaviour.Door;
                case Block.Door_Water: 
                    return WalkthroughBehaviour.Door;
                case Block.Door_Lava: 
                    return WalkthroughBehaviour.Door;
                case Block.Train: 
                    return WalkthroughBehaviour.Train;
            }
            if (props[block].IsMessageBlock && nonSolid)
            {
                return WalkthroughBehaviour.DoMessageBlock;
            }
            if (props[block].IsPortal && nonSolid)
            {
                return WalkthroughBehaviour.DoPortal;
            }
            return null;
        }
        public static HandlePhysics GetPhysicsHandler(ushort block, BlockProps[] props)
        {
            switch (block)
            {
                case Block.Door_Log_air: 
                    return DoorPhysics.Do;
                case Block.Door_TNT_air: 
                    return DoorPhysics.Do;
                case Block.Door_Green_air: 
                    return DoorPhysics.Do;
                case Block.SnakeTail: 
                    return SnakePhysics.DoTail;
                case Block.Snake: 
                    return SnakePhysics.Do;
                case Block.RocketHead: 
                    return RocketPhysics.Do;
                case Block.Fireworks:
                    return FireworkPhysics.Do;
                case Block.ZombieBody: 
                    return ZombiePhysics.Do;
                case Block.ZombieHead: 
                    return ZombiePhysics.DoHead;
                case Block.Creeper:
                    return ZombiePhysics.Do;
                case Block.Water: 
                    return SimpleLiquidPhysics.DoWater;
                case Block.Deadly_ActiveWater: 
                    return SimpleLiquidPhysics.DoWater;
                case Block.Lava: 
                    return SimpleLiquidPhysics.DoLava;
                case Block.Deadly_ActiveLava: 
                    return SimpleLiquidPhysics.DoLava;
                case Block.WaterDown: 
                    return ExtLiquidPhysics.DoWaterfall;
                case Block.LavaDown: 
                    return ExtLiquidPhysics.DoLavafall;
                case Block.WaterFaucet:
                    return (Level lvl, ref PhysInfo C) =>
                    ExtLiquidPhysics.DoFaucet(lvl, ref C, Block.WaterDown);
                case Block.LavaFaucet:
                    return (Level lvl, ref PhysInfo C) =>
                    ExtLiquidPhysics.DoFaucet(lvl, ref C, Block.LavaDown);
                case Block.FiniteWater: 
                    return FinitePhysics.DoWaterOrLava;
                case Block.FiniteLava: 
                    return FinitePhysics.DoWaterOrLava;
                case Block.FiniteFaucet: 
                    return FinitePhysics.DoFaucet;
                case Block.Magma: 
                    return ExtLiquidPhysics.DoMagma;
                case Block.Geyser: 
                    return ExtLiquidPhysics.DoGeyser;
                case Block.FastLava: 
                    return SimpleLiquidPhysics.DoFastLava;
                case Block.Deadly_FastLava: 
                    return SimpleLiquidPhysics.DoFastLava;
                case Block.Air: 
                    return AirPhysics.DoAir;
                case Block.Leaves: 
                    return LeafPhysics.DoLeaf;
                case Block.Log: 
                    return LeafPhysics.DoLog;
                case Block.Sapling: 
                    return OtherPhysics.DoShrub;
                case Block.Fire:
                    return FirePhysics.Do;
                case Block.LavaFire: 
                    return FirePhysics.Do;
                case Block.Sand: 
                    return OtherPhysics.DoFalling;
                case Block.Gravel: 
                    return OtherPhysics.DoFalling;
                case Block.FloatWood: 
                    return OtherPhysics.DoFloatwood;
                case Block.Sponge:
                    return (Level lvl, ref PhysInfo C) =>
                    OtherPhysics.DoSponge(lvl, ref C, false);
                case Block.LavaSponge:
                    return (Level lvl, ref PhysInfo C) =>
                    OtherPhysics.DoSponge(lvl, ref C, true);
                case Block.Air_Flood:
                    return (Level lvl, ref PhysInfo C) =>
                    AirPhysics.DoFlood(lvl, ref C, AirFlood.Full, Block.Air_Flood);
                case Block.Air_FloodLayer:
                    return (Level lvl, ref PhysInfo C) =>
                    AirPhysics.DoFlood(lvl, ref C, AirFlood.Layer, Block.Air_FloodLayer);
                case Block.Air_FloodDown:
                    return (Level lvl, ref PhysInfo C) =>
                    AirPhysics.DoFlood(lvl, ref C, AirFlood.Down, Block.Air_FloodDown);
                case Block.Air_FloodUp:
                    return (Level lvl, ref PhysInfo C) =>
                    AirPhysics.DoFlood(lvl, ref C, AirFlood.Up, Block.Air_FloodUp);
                case Block.TNT_Small:
                    return TntPhysics.DoSmallTnt;
                case Block.TNT_Big: 
                    return TntPhysics.DoBigTnt;
                case Block.TNT_Nuke: 
                    return TntPhysics.DoNukeTnt;
                case Block.TNT_Explosion: 
                    return TntPhysics.DoTntExplosion;
                case Block.Train: 
                    return TrainPhysics.Do;
            }
            HandlePhysics animalAI = AnimalAIHandler(props[block].AnimalAI);
            if (animalAI != null)
            {
                return animalAI;
            }
            if (props[block].oDoorBlock != Block.Invalid)
            {
                return DoorPhysics.ODoor;
            }
            if (props[block].GrassBlock != Block.Invalid)
            {
                return OtherPhysics.DoDirtGrow;
            }
            if (props[block].DirtBlock != Block.Invalid)
            {
                return OtherPhysics.DoGrassDie;
            }
            if ((block >= Block.Red && block <= Block.RedMushroom) || block == Block.Wood || block == Block.Log || block == Block.Bookshelf)
            {
                return OtherPhysics.DoOther;
            }
            return null;
        }
        public static HandlePhysics GetPhysicsDoorsHandler(ushort block, BlockProps[] props)
        {
            if (block == Block.Air)
            {
                return DoorPhysics.Do;
            }
            if (block == Block.Door_Log_air)
            {
                return DoorPhysics.Do;
            }
            if (block == Block.Door_TNT_air)
            {
                return DoorPhysics.Do;
            }
            if (block == Block.Door_Green_air)
            {
                return DoorPhysics.Do;
            }
            if (props[block].oDoorBlock != Block.Invalid)
            {
                return DoorPhysics.ODoor;
            }
            return null;
        }
        public static HandlePhysics AnimalAIHandler(AnimalAI ai)
        {
            if (ai == AnimalAI.Fly)
            {
                return BirdPhysics.Do;
            }
            if (ai == AnimalAI.FleeAir)
            {
                return (Level lvl, ref PhysInfo C) => HunterPhysics.Do(lvl, ref C, Block.Air, -1);
            }
            else if (ai == AnimalAI.FleeWater)
            {
                return (Level lvl, ref PhysInfo C) => HunterPhysics.Do(lvl, ref C, Block.Water, -1);
            }
            else if (ai == AnimalAI.FleeLava)
            {
                return (Level lvl, ref PhysInfo C) => HunterPhysics.Do(lvl, ref C, Block.Lava, -1);
            }
            if (ai == AnimalAI.KillerAir)
            {
                return (Level lvl, ref PhysInfo C) => HunterPhysics.Do(lvl, ref C, Block.Air, 1);
            }
            else if (ai == AnimalAI.KillerWater)
            {
                return (Level lvl, ref PhysInfo C) => HunterPhysics.Do(lvl, ref C, Block.Water, 1);
            }
            else if (ai == AnimalAI.KillerLava)
            {
                return (Level lvl, ref PhysInfo C) => HunterPhysics.Do(lvl, ref C, Block.Lava, 1);
            }
            return null;
        }
    }
}