//TODO: Update this for ClassiCube .cw file, 
// Current code is from https://github.com/RandomStrangers/ClassicalSharp/tree/master/ClassicalSharp, which is severely outdated.
using System;
using System.IO;
using System.IO.Compression;
using MAX.Events.LevelEvents;
using System.Drawing;
using System.Runtime.InteropServices;
using fNbt;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace MAX.Levels.IO {
	
	/// <summary> Structure that can be used for quick manipulations of A/R/G/B colours. </summary>
	/// <remarks> This structure is suitable for interop with OpenGL or Direct3D.
	/// The order of each colour component differs depending on the underlying API. </remarks>
	[StructLayout(LayoutKind.Explicit)]
	public struct PackedCol : IEquatable<PackedCol> {
		
		[FieldOffset(0)] public uint Packed;
		[FieldOffset(0)] public byte R;
		[FieldOffset(1)] public byte G;
		[FieldOffset(2)] public byte B;
		[FieldOffset(3)] public byte A;
		
		public PackedCol(byte r, byte g, byte b, byte a) {
			Packed = 0;
			A = a; R = r; G = g; B = b;
		}
		
		public PackedCol(int r, int g, int b, int a) {
			Packed = 0;
			A = (byte)a; R = (byte)r; G = (byte)g; B = (byte)b;
		}
		
		public PackedCol(byte r, byte g, byte b) {
			Packed = 0;
			A = 255; R = r; G = g; B = b;
		}
		
		public PackedCol(int r, int g, int b) {
			Packed = 0;
			A = 255; R = (byte)r; G = (byte)g; B = (byte)b;
		}
		
		/// <summary> Multiplies the RGB components of this instance by the
		/// specified t parameter, where 0 ≤ t ≤ 1 </summary>
		public static PackedCol Scale(PackedCol value, float t) {
			value.R = (byte)(value.R * t);
			value.G = (byte)(value.G * t);
			value.B = (byte)(value.B * t);
			return value;
		}
		/// <summary> Performs linear interpolation between two values. </summary>
		public static float LerpFloat(float a, float b, float t) {
			return a + (b - a) * t;
		}
		/// <summary> Linearly interpolates the RGB components of the two colours
		/// by the specified t parameter, where 0 ≤ t ≤ 1 </summary>
		public static PackedCol Lerp(PackedCol a, PackedCol b, float t) {
			a.R = (byte)LerpFloat(a.R, b.R, t);
			a.G = (byte)LerpFloat(a.G, b.G, t);
			a.B = (byte)LerpFloat(a.B, b.B, t);
			return a;
		}
		
		public static PackedCol GetHexEncodedCol(int hex, int lo, int hi) {
			return new PackedCol(
				lo * ((hex >> 2) & 1) + hi * (hex >> 3),
				lo * ((hex >> 1) & 1) + hi * (hex >> 3),
				lo * ((hex >> 0) & 1) + hi * (hex >> 3));
		}

		public const float ShadeX = 0.6f, ShadeZ = 0.8f, ShadeYBottom = 0.5f;
		public static void GetShaded(PackedCol normal, out PackedCol xSide,
		                             out PackedCol zSide, out PackedCol yBottom) {
			xSide = PackedCol.Scale(normal, ShadeX);
			zSide = PackedCol.Scale(normal, ShadeZ);
			yBottom = PackedCol.Scale(normal, ShadeYBottom);
		}

		
		/// <summary> Packs this instance into a 32 bit integer, where A occupies
		/// the highest 8 bits and B occupies the lowest 8 bits. </summary>
		public int ToArgb() { return A << 24 | R << 16 | G << 8 | B; }
		
		public static PackedCol Argb(int c) {
			PackedCol col = default(PackedCol);
			col.A = (byte)(c >> 24);
			col.R = (byte)(c >> 16);
			col.G = (byte)(c >> 8);
			col.B = (byte)c;
			return col;
		}
		
		public override bool Equals(object obj) {
			return (obj is PackedCol) && Equals((PackedCol)obj);
		}
		
		public bool Equals(PackedCol other) { return Packed == other.Packed; }
		public override int GetHashCode() { return (int)Packed; }
		
		public override string ToString() {
			return R + ", " + G + ", " + B + " : " + A;
		}
		
		public static bool operator == (PackedCol left, PackedCol right) {
			return left.Packed == right.Packed;
		}
		
		public static bool operator != (PackedCol left, PackedCol right) {
			return left.Packed != right.Packed;
		}
		
		public static PackedCol operator * (PackedCol left, PackedCol right) {
			left.R = (byte)((left.R * right.R) / 255);
			left.G = (byte)((left.G * right.G) / 255);
			left.B = (byte)((left.B * right.B) / 255);
			return left;
		}
		
		public static implicit operator Color(PackedCol col) {
			return Color.FromArgb(col.A, col.R, col.G, col.B);
		}
		
		public static PackedCol Red   = new PackedCol(255, 0, 0);
		public static PackedCol Green = new PackedCol(0, 255, 0);
		public static PackedCol Blue  = new PackedCol(0, 0, 255);
		
		public static PackedCol White = new PackedCol(255, 255, 255);
		public static PackedCol Black = new PackedCol(0, 0, 0);

		public static PackedCol Yellow  = new PackedCol(255, 255, 0);
		public static PackedCol Magenta = new PackedCol(255, 0, 255);
		public static PackedCol Cyan    = new PackedCol(0, 255, 255);
		
		public string ToHex() {
			byte[] array = new byte[] { R, G, B };
			int len = array.Length;
			char[] hex = new char[len * 2];
			
			for (int i = 0; i < array.Length; i++) {
				int value = array[i], hi = value >> 4, lo = value & 0x0F;
				// 48 = index of 0, 55 = index of (A - 10)
				hex[i * 2 + 0] = hi < 10 ? (char)(hi + 48) : (char)(hi + 55);
				hex[i * 2 + 1] = lo < 10 ? (char)(lo + 48) : (char)(lo + 55);
			}
			return new String(hex);
		}
		
		public static bool TryParse(string input, out PackedCol value) {
			value = default(PackedCol);
			if (input == null || input.Length < 6) return false;
			if (input.Length > 6 && (input[0] != '#' || input.Length > 7)) return false;
			
			int rH, rL, gH, gL, bH, bL;
			int i = input[0] == '#' ? 1 : 0;
			
			if (!UnHex(input[i + 0], out rH) || !UnHex(input[i + 1], out rL)) return false;
			if (!UnHex(input[i + 2], out gH) || !UnHex(input[i + 3], out gL)) return false;
			if (!UnHex(input[i + 4], out bH) || !UnHex(input[i + 5], out bL)) return false;
			
			value = new PackedCol((rH << 4) | rL, (gH << 4) | gL, (bH << 4) | bL);
			return true;
		}
		
		public static PackedCol Parse(string input) {
			PackedCol value;
			if (!TryParse(input, out value)) throw new FormatException();
			return value;
		}
		
		public static bool UnHex(char hex, out int value) {
			value = 0;
			if (hex >= '0' && hex <= '9') {
				value = (hex - '0');
			} else if (hex >= 'a' && hex <= 'f') {
				value = (hex - 'a') + 10;
			} else if (hex >= 'A' && hex <= 'F') {
				value = (hex - 'A') + 10;
			} else {
				return false;
			}
			return true;
		}
	}
}
namespace MAX.Levels.IO {
	public struct NbtTag2 {
		public string Name;
		public object Value;
		public NbtTagType TagId;
	}
	
	public class NbtList2 {
		public NbtTagType ChildTagId;
		public object[] ChildrenValues;
	}
	

	public class NbtFile2 {
		
		public NbtBinaryReader reader;
		public BinaryWriter writer;
		
		public NbtFile2(BinaryReader reader) 
		{
		}
		
		public NbtFile2(BinaryWriter writer) {
			this.writer = writer;
		}
		
		
		public void Write(NbtTagType v) { writer.Write((byte)v); }
		
		public void Write(NbtTagType v, string name) { writer.Write((byte)v); Write(name); }
		
		public void WriteInt32(int v) { writer.Write(IPAddress.HostToNetworkOrder(v)); }
		
		public void WriteInt16(short v) { writer.Write(IPAddress.HostToNetworkOrder(v)); }
		
		public void WriteUInt8(int v) { writer.Write((byte)v); }
		
		public void WriteUInt8(byte v) { writer.Write(v); }
		
		public void WriteBytes(byte[] v) { writer.Write(v); }
		
		public void WriteBytes(byte[] v, int index, int count) { writer.Write(v, index, count); }
		
		public void Write(string value) {
			byte[] data = Encoding.UTF8.GetBytes(value);
			WriteInt16((short)data.Length);
			writer.Write(data);
		}
		
		public void WriteCpeExtCompound(string name, int version) {
			Write(NbtTagType.Compound, name);		
			Write(NbtTagType.Int, "ExtensionVersion"); 
			WriteInt32(version);
		}
		
		
		public long ReadInt64() { return IPAddress.HostToNetworkOrder(reader.ReadInt64()); }
		
		public int ReadInt32() { return IPAddress.HostToNetworkOrder(reader.ReadInt32()); }
		
		public short ReadInt16() { return IPAddress.HostToNetworkOrder(reader.ReadInt16()); }
		
		public string ReadString() {
			int len = (ushort)ReadInt16();
			byte[] data = reader.ReadBytes(len);
			return Encoding.UTF8.GetString(data);
		}
		
		public unsafe NbtTag2 ReadTag(byte typeId, bool readTagName) {
			NbtTag2 tag = default(NbtTag2);
			if (typeId == 0) return tag;
			
			tag.Name = readTagName ? ReadString() : null;
			tag.TagId = (NbtTagType)typeId;			
			switch ((NbtTagType)typeId) {
				case NbtTagType.Byte:
					tag.Value = reader.ReadByte(); break;
				case NbtTagType.Short:
					tag.Value = ReadInt16(); break;
				case NbtTagType.Int:
					tag.Value = ReadInt32(); break;
				case NbtTagType.Long:
					tag.Value = ReadInt64(); break;
				case NbtTagType.Float:
					int temp32 = ReadInt32();
					tag.Value = *((float*)&temp32); break;
				case NbtTagType.Double:
					long temp64 = ReadInt64();
					tag.Value = *((double*)&temp64); break;
				case NbtTagType.ByteArray:
					tag.Value = reader.ReadBytes(ReadInt32()); break;
				case NbtTagType.String:
					tag.Value = ReadString(); break;
					
				/*case NbtTagType.List:
					NbtList2 list = new NbtList2();
					list.ChildTagId = (NbtTagType)reader.ReadByte();
					list.ChildrenValues = new object[ReadInt32()];
					for (int i = 0; i < list.ChildrenValues.Length; i++) {
						list.ChildrenValues[i] = ReadTag((byte)list.ChildTagId, false).Value;
					}
					tag.Value = list; break;*/
				case NbtTagType.List:
					NbtList list = new NbtList();
					list.ListType = (NbtTagType)reader.ReadByte();
					var tags = new NbtTagType[ReadInt32()];
					list.Tags.AddRange(tags);
					for (int i = 0; i < list.ChildrenValues.COunt; i++) 
					{
						list.ChildrenValues[i] = ReadTag((byte)list.ListType, false).Value;
					}
					tag.Value = list; 
					break;
				case NbtTagType.Compound:
					byte childTagId;
					Dictionary<string, NbtTag2> children = new Dictionary<string, NbtTag2>();
					while ((childTagId = reader.ReadByte()) != (byte)NbtTagType.End) {
						NbtTag2 child = ReadTag(childTagId, true); children[child.Name] = child;
					}
					tag.Value = children; break;
					
				case NbtTagType.IntArray:
					int[] array = new int[ReadInt32()];
					for (int i = 0; i < array.Length; i++) {
						array[i] = ReadInt32();
					}
					tag.Value = array; break;
					
				default:
					throw new InvalidDataException("Unrecognised tag id: " + typeId);
			}
			return tag;
		}
	}
    public unsafe class CwExporter : IMapExporter
    {
        public override string Extension { get { return ".cw"; } }
        public override void Write(Stream dst, Level lvl)
        {
            using (Stream gs = new GZipStream(dst, CompressionMode.Compress))
            {
                SaveCW(gs, lvl);
            }
        }
		BinaryWriter writer;
		NbtFile2 nbt;
		
		public void SaveCW(Stream s, Level lvl) 
  		{
				writer = new BinaryWriter(s);
				nbt = new NbtFile2(writer);
				
				nbt.Write(NbtTagType.Compound, "ClassicWorld");
				
				nbt.Write(NbtTagType.Byte, "FormatVersion"); 
				nbt.WriteUInt8(1);
				
				nbt.Write(NbtTagType.Short, "X"); 
				nbt.WriteInt16((short)lvl.Width);
				
				nbt.Write(NbtTagType.Short, "Y"); 
				nbt.WriteInt16((short)lvl.Height);
				
				nbt.Write(NbtTagType.Short, "Z"); 
				nbt.WriteInt16((short)lvl.Length);
				
				WriteSpawnCompoundTag(lvl);
				
				nbt.Write(NbtTagType.ByteArray, "BlockArray"); 
				nbt.WriteInt32(lvl.blocks.Length);
				nbt.WriteBytes(lvl.blocks);
				
				WriteMetadata(lvl);
				
				nbt.Write(NbtTagType.End);
		}
		
		void WriteSpawnCompoundTag(Level lvl) 
  		{
			nbt.Write(NbtTagType.Compound, "Spawn");
			
			nbt.Write(NbtTagType.Short, "X"); 
			nbt.WriteInt16((short)lvl.spawnx);
			
			nbt.Write(NbtTagType.Short, "Y"); 
			nbt.WriteInt16((short)lvl.spawny);
			
			nbt.Write(NbtTagType.Short, "Z"); 
			nbt.WriteInt16((short)lvl.spawnz);
			
			nbt.Write(NbtTagType.Byte, "H");
			nbt.WriteUInt8(lvl.roty);
			
			nbt.Write(NbtTagType.Byte, "P");
			nbt.WriteUInt8(lvl.rotx);
			
			nbt.Write(NbtTagType.End);
		}
		
		void WriteMetadata(Level lvl) 
  		{
			nbt.Write(NbtTagType.Compound, "Metadata");
			nbt.Write(NbtTagType.Compound, "CPE");

			nbt.WriteCpeExtCompound("ClickDistance", 1);
			nbt.Write(NbtTagType.Short, "Distance"); 
			nbt.WriteInt16((short)(0));
			nbt.Write(NbtTagType.End);
			
			nbt.WriteCpeExtCompound("EnvWeatherType", 1);
			nbt.Write(NbtTagType.Byte, "WeatherType"); 
			nbt.WriteUInt8((byte)lvl.Config.Weather);
			nbt.Write(NbtTagType.End);
			
			nbt.WriteCpeExtCompound("EnvMapAppearance", 1);
			nbt.Write(NbtTagType.Byte, "SideBlock"); 
			nbt.WriteUInt8(lvl.Config.EdgeBlock);
			nbt.Write(NbtTagType.Byte, "EdgeBlock"); 
			nbt.WriteUInt8(lvl.Config.HorizonBlock);
			nbt.Write(NbtTagType.Short, "SideLevel"); 
			nbt.WriteInt16((short)lvl.Config.EdgeLevel);
			nbt.Write(NbtTagType.String, "TextureURL");
			string url = lvl.Config.TexturePack == null ? "" : lvl.Config.TexturePack;
			nbt.Write(url);
			nbt.Write(NbtTagType.End);
			
			nbt.WriteCpeExtCompound("EnvColors", 1);
			WriteColCompound("Sky", PackedCol.Parse(lvl.Config.SkyColor));
			WriteColCompound("Cloud", PackedCol.Parse(lvl.Config.CloudColor));
			WriteColCompound("Fog", PackedCol.Parse(lvl.Config.FogColor));
			WriteColCompound("Ambient", PackedCol.Parse(lvl.Config.ShadowColor));
			WriteColCompound("Sunlight", PackedCol.Parse(lvl.Config.LightColor));
			nbt.Write(NbtTagType.End);
			
			nbt.WriteCpeExtCompound("BlockDefinitions", 1);
			for (int block = 1; block < 256; block++) 
   			{
				WriteBlockDefinitionCompound(lvl, (byte)block);
			}
			nbt.Write(NbtTagType.End);
			
			nbt.Write(NbtTagType.End);
			nbt.Write(NbtTagType.End);
    		}
		
		void WriteColCompound(string name, PackedCol col) 
  		{
			nbt.Write(NbtTagType.Compound, name);
			
			nbt.Write(NbtTagType.Short, "R"); 
			nbt.WriteInt16(col.R);
			nbt.Write(NbtTagType.Short, "G"); 
			nbt.WriteInt16(col.G);
			nbt.Write(NbtTagType.Short, "B"); 
			nbt.WriteInt16(col.B);
			
			nbt.Write(NbtTagType.End);
		}
		
		unsafe void WriteBlockDefinitionCompound(Level lvl, byte id) 
  		{
			nbt.Write(NbtTagType.Compound, "Block" + id);
            		BlockDefinition def = lvl.GetBlockDef(id);
			nbt.Write(NbtTagType.Byte, "ID"); 
			nbt.WriteUInt8(id);
			nbt.Write(NbtTagType.String, "Name"); 
			nbt.Write(def.Name);
			nbt.Write(NbtTagType.Byte, "CollideType"); 
			nbt.WriteUInt8((byte)def.CollideType);
			float speed = def.Speed;
			nbt.Write(NbtTagType.Float, "Speed"); 
			nbt.WriteInt32(*((int*)&speed));
			
			nbt.Write(NbtTagType.ByteArray, "Textures"); 
			nbt.WriteInt32(6);
			nbt.WriteUInt8(def.TopTex);
			nbt.WriteUInt8(def.BottomTex);
			nbt.WriteUInt8(def.LeftTex);
			nbt.WriteUInt8(def.RightTex);
			nbt.WriteUInt8(def.FrontTex);
			nbt.WriteUInt8(def.BackTex);
			
			nbt.Write(NbtTagType.Byte, "TransmitsLight"); 
      			byte light = 0;
   			if (def.BlocksLight == true)
      			{
      				light = 1;
	  		}
			nbt.WriteUInt8(light);
			nbt.Write(NbtTagType.Byte, "WalkSound"); 
			nbt.WriteUInt8(def.WalkSound);
   			byte bright = 0;
   			if (def.FullBright == true)
      			{
      				bright = 1;
	  		}
			nbt.Write(NbtTagType.Byte, "FullBright"); 
			nbt.WriteUInt8(bright);
									
			nbt.Write(NbtTagType.Byte, "Shape");
			nbt.WriteUInt8(def.Shape);			
			nbt.Write(NbtTagType.Byte, "BlockDraw");
			nbt.WriteUInt8(def.BlockDraw);
			
			nbt.Write(NbtTagType.ByteArray, "Fog"); 
			nbt.WriteInt32(4);
			nbt.WriteUInt8(def.FogDensity);
			nbt.WriteUInt8(def.FogR); 
   			nbt.WriteUInt8(def.FogG); 
      			nbt.WriteUInt8(def.FogB);
			
			nbt.Write(NbtTagType.ByteArray, "Coords"); 
			nbt.WriteInt32(6);
			nbt.WriteUInt8((byte)(def.MinX * 16)); nbt.WriteUInt8((byte)(def.MinY * 16)); 
			nbt.WriteUInt8((byte)(def.MinZ * 16)); nbt.WriteUInt8((byte)(def.MaxX * 16));
			nbt.WriteUInt8((byte)(def.MaxY * 16)); nbt.WriteUInt8((byte)(def.MaxZ * 16));
			
			nbt.Write(NbtTagType.End);
		}
  	}
}
