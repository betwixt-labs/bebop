using Bebop;
namespace Test {
  public abstract class IM {
    #nullable enable
    public float? A { get; set; }
    public double? B { get; set; }
    #nullable disable
  }

  /// <inheritdoc />
  public class M : IM {
    [System.Runtime.CompilerServices.MethodImpl(BebopView.HotPath)]
    public static byte[] Encode(IM message) {
      var view = new BebopView();
      EncodeInto(message, ref view);
      return view.ToArray();
    }
    [System.Runtime.CompilerServices.MethodImpl(BebopView.HotPath)]
    public static void EncodeInto(IM message, ref BebopView view) {
      var pos = view.ReserveMessageLength();
      var start = view.Length;

      if (message.A.HasValue) {
        view.WriteByte(1);
        view.WriteFloat32(message.A.Value);
      }

      if (message.B.HasValue) {
        view.WriteByte(2);
        view.WriteFloat64(message.B.Value);
      }
      view.WriteByte(0);
      var end = view.Length;
      view.FillMessageLength(pos, unchecked((uint) unchecked(end - start)));
    }

    [System.Runtime.CompilerServices.MethodImpl(BebopView.HotPath)]
    public static IM DecodeFrom(ref BebopView view) {
      var message = new M();
      var length = view.ReadMessageLength();
      var end = unchecked((int) (view.Position + length));
      while (true) {
        switch (view.ReadByte()) {
          case 0:
            return message;
          case 1:
              message.A = view.ReadFloat32();
              break;
          case 2:
              message.B = view.ReadFloat64();
              break;
          default:
              view.Position = end;
              return message;
        }
      }
    }
  }
  public abstract class IS {
    public int X { get; set; }
    public int Y { get; set; }
  }

  /// <inheritdoc />
  public class S : IS {
    [System.Runtime.CompilerServices.MethodImpl(BebopView.HotPath)]
    public static byte[] Encode(IS message) {
      var view = new BebopView();
      EncodeInto(message, ref view);
      return view.ToArray();
    }
    [System.Runtime.CompilerServices.MethodImpl(BebopView.HotPath)]
    public static void EncodeInto(IS message, ref BebopView view) {
      view.WriteInt32(message.X);
      view.WriteInt32(message.Y);
    }

    [System.Runtime.CompilerServices.MethodImpl(BebopView.HotPath)]
    public static IS DecodeFrom(ref BebopView view) {
      int field0;
      field0 = view.ReadInt32();
      int field1;
      field1 = view.ReadInt32();
      return new S {
        X = field0,
        Y = field1,
      };
    }
  }
  public abstract class ISomeMaps {
    public System.Collections.Generic.Dictionary<bool, bool> M1 { get; set; }
    public System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, string>> M2 { get; set; }
    public System.Collections.Generic.Dictionary<int, System.Collections.Generic.Dictionary<bool, IS>[]>[] M3 { get; set; }
    public System.Collections.Generic.Dictionary<string, float[]>[] M4 { get; set; }
    public System.Collections.Generic.Dictionary<System.Guid, IM> M5 { get; set; }
  }

  /// <inheritdoc />
  public class SomeMaps : ISomeMaps {
    [System.Runtime.CompilerServices.MethodImpl(BebopView.HotPath)]
    public static byte[] Encode(ISomeMaps message) {
      var view = new BebopView();
      EncodeInto(message, ref view);
      return view.ToArray();
    }
    [System.Runtime.CompilerServices.MethodImpl(BebopView.HotPath)]
    public static void EncodeInto(ISomeMaps message, ref BebopView view) {
      view.WriteUInt32(unchecked((uint)message.M1.Count));
      foreach (var kv0 in message.M1) {
        view.WriteByte(kv0.Key);
        view.WriteByte(kv0.Value);
      }
      view.WriteUInt32(unchecked((uint)message.M2.Count));
      foreach (var kv0 in message.M2) {
        view.WriteString(kv0.Key);
        view.WriteUInt32(unchecked((uint)kv0.Value.Count));
        foreach (var kv1 in kv0.Value) {
          view.WriteString(kv1.Key);
          view.WriteString(kv1.Value);
        }
      }
      {
        var length0 = unchecked((uint)message.M3.Length);
        view.WriteUInt32(length0);
        for (var i0 = 0; i0 < length0; i0++) {
          view.WriteUInt32(unchecked((uint)message.M3[i0].Count));
          foreach (var kv1 in message.M3[i0]) {
            view.WriteInt32(kv1.Key);
            {
              var length2 = unchecked((uint)kv1.Value.Length);
              view.WriteUInt32(length2);
              for (var i2 = 0; i2 < length2; i2++) {
                view.WriteUInt32(unchecked((uint)kv1.Value[i2].Count));
                foreach (var kv3 in kv1.Value[i2]) {
                  view.WriteByte(kv3.Key);
                  Test.S.EncodeInto(kv3.Value, ref view);
                }
              }
            }
          }
        }
      }
      {
        var length0 = unchecked((uint)message.M4.Length);
        view.WriteUInt32(length0);
        for (var i0 = 0; i0 < length0; i0++) {
          view.WriteUInt32(unchecked((uint)message.M4[i0].Count));
          foreach (var kv1 in message.M4[i0]) {
            view.WriteString(kv1.Key);
            view.WriteFloat32s(kv1.Value);
          }
        }
      }
      view.WriteUInt32(unchecked((uint)message.M5.Count));
      foreach (var kv0 in message.M5) {
        view.WriteGuid(kv0.Key);
        Test.M.EncodeInto(kv0.Value, ref view);
      }
    }

    [System.Runtime.CompilerServices.MethodImpl(BebopView.HotPath)]
    public static ISomeMaps DecodeFrom(ref BebopView view) {
      System.Collections.Generic.Dictionary<bool, bool> field0;
      [System.Runtime.CompilerServices.MethodImpl(BebopView.HotPath)]
      static System.Collections.Generic.Dictionary<bool, bool> DecodeM1(ref BebopView view) {
        System.Collections.Generic.Dictionary<bool, bool> x;
        {
          var length0 = unchecked((int)view.ReadUInt32());
          x = new System.Collections.Generic.Dictionary<bool, bool>(length0);
          for (var i0 = 0; i0 < length0; i0++) {
            bool k0;
            bool v0;
            k0 = view.ReadByte() != 0;
            v0 = view.ReadByte() != 0;
            x.Add(k0, v0);
          }
        }
        return x;
      }
      field0 = DecodeM1(ref view);
      System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, string>> field1;
      [System.Runtime.CompilerServices.MethodImpl(BebopView.HotPath)]
      static System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, string>> DecodeM2(ref BebopView view) {
        System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, string>> x;
        {
          var length0 = unchecked((int)view.ReadUInt32());
          x = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, string>>(length0);
          for (var i0 = 0; i0 < length0; i0++) {
            string k0;
            System.Collections.Generic.Dictionary<string, string> v0;
            k0 = view.ReadString();
            {
              var length1 = unchecked((int)view.ReadUInt32());
              v0 = new System.Collections.Generic.Dictionary<string, string>(length1);
              for (var i1 = 0; i1 < length1; i1++) {
                string k1;
                string v1;
                k1 = view.ReadString();
                v1 = view.ReadString();
                v0.Add(k1, v1);
              }
            }
            x.Add(k0, v0);
          }
        }
        return x;
      }
      field1 = DecodeM2(ref view);
      System.Collections.Generic.Dictionary<int, System.Collections.Generic.Dictionary<bool, IS>[]>[] field2;
      [System.Runtime.CompilerServices.MethodImpl(BebopView.HotPath)]
      static System.Collections.Generic.Dictionary<int, System.Collections.Generic.Dictionary<bool, IS>[]>[] DecodeM3(ref BebopView view) {
        System.Collections.Generic.Dictionary<int, System.Collections.Generic.Dictionary<bool, IS>[]>[] x;
        {
          var length0 = unchecked((int)view.ReadUInt32());
          x = new System.Collections.Generic.Dictionary<int, System.Collections.Generic.Dictionary<bool, IS>[]>[length0];
          for (var i0 = 0; i0 < length0; i0++) {
            System.Collections.Generic.Dictionary<int, System.Collections.Generic.Dictionary<bool, IS>[]> x0;
            {
              var length1 = unchecked((int)view.ReadUInt32());
              x0 = new System.Collections.Generic.Dictionary<int, System.Collections.Generic.Dictionary<bool, IS>[]>(length1);
              for (var i1 = 0; i1 < length1; i1++) {
                int k1;
                System.Collections.Generic.Dictionary<bool, IS>[] v1;
                k1 = view.ReadInt32();
                {
                  var length2 = unchecked((int)view.ReadUInt32());
                  v1 = new System.Collections.Generic.Dictionary<bool, IS>[length2];
                  for (var i2 = 0; i2 < length2; i2++) {
                    System.Collections.Generic.Dictionary<bool, IS> x2;
                    {
                      var length3 = unchecked((int)view.ReadUInt32());
                      x2 = new System.Collections.Generic.Dictionary<bool, IS>(length3);
                      for (var i3 = 0; i3 < length3; i3++) {
                        bool k3;
                        IS v3;
                        k3 = view.ReadByte() != 0;
                        v3 = Test.S.DecodeFrom(ref view);
                        x2.Add(k3, v3);
                      }
                    }
                    v1[i2] = x2;
                  }
                }
                x0.Add(k1, v1);
              }
            }
            x[i0] = x0;
          }
        }
        return x;
      }
      field2 = DecodeM3(ref view);
      System.Collections.Generic.Dictionary<string, float[]>[] field3;
      [System.Runtime.CompilerServices.MethodImpl(BebopView.HotPath)]
      static System.Collections.Generic.Dictionary<string, float[]>[] DecodeM4(ref BebopView view) {
        System.Collections.Generic.Dictionary<string, float[]>[] x;
        {
          var length0 = unchecked((int)view.ReadUInt32());
          x = new System.Collections.Generic.Dictionary<string, float[]>[length0];
          for (var i0 = 0; i0 < length0; i0++) {
            System.Collections.Generic.Dictionary<string, float[]> x0;
            {
              var length1 = unchecked((int)view.ReadUInt32());
              x0 = new System.Collections.Generic.Dictionary<string, float[]>(length1);
              for (var i1 = 0; i1 < length1; i1++) {
                string k1;
                float[] v1;
                k1 = view.ReadString();
                {
                  var length2 = unchecked((int)view.ReadUInt32());
                  v1 = new float[length2];
                  for (var i2 = 0; i2 < length2; i2++) {
                    float x2;
                    x2 = view.ReadFloat32();
                    v1[i2] = x2;
                  }
                }
                x0.Add(k1, v1);
              }
            }
            x[i0] = x0;
          }
        }
        return x;
      }
      field3 = DecodeM4(ref view);
      System.Collections.Generic.Dictionary<System.Guid, IM> field4;
      [System.Runtime.CompilerServices.MethodImpl(BebopView.HotPath)]
      static System.Collections.Generic.Dictionary<System.Guid, IM> DecodeM5(ref BebopView view) {
        System.Collections.Generic.Dictionary<System.Guid, IM> x;
        {
          var length0 = unchecked((int)view.ReadUInt32());
          x = new System.Collections.Generic.Dictionary<System.Guid, IM>(length0);
          for (var i0 = 0; i0 < length0; i0++) {
            System.Guid k0;
            IM v0;
            k0 = view.ReadGuid();
            v0 = Test.M.DecodeFrom(ref view);
            x.Add(k0, v0);
          }
        }
        return x;
      }
      field4 = DecodeM5(ref view);
      return new SomeMaps {
        M1 = field0,
        M2 = field1,
        M3 = field2,
        M4 = field3,
        M5 = field4,
      };
    }
  }
}
