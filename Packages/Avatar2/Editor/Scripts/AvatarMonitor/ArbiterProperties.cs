using System;
using System.Text;
using System.Collections.Generic;

using System.Runtime.InteropServices;

using FlatBuffers;

#pragma warning disable 0649

namespace Oculus.Avatar2
{
    // Structures filled from Arbiter packets.
    // These are here to make an indirection between the flatbuffer
    // structs and the code talking to them, to make it simpler
    // to change out the underlying transmission mechanism.
    // Arguably we can use things like ovrVector3f, or Unity Vector3 etc.
    // Using Unity types means binding this code to Unity. Using ovr types
    // maybe makes more sense, but also prevents this code being a standalone
    // C# component, which is tempting for testing.
    public class ArbiterStructs
    {
        public struct Vec3
        {
            public float x, y, z;
        };

        public struct Vec4
        {
            public float x, y, z, w;
        };

        public struct Quat
        {
            public float x, y, z, w;
        };

        public struct JointParams
        {
            public Vec3 translation;
            public Quat rotation;
            public Vec3 scale;
        };

        public struct LODRecord
        {
            public Int32 id;
            public Int32 importanceScore;
            public Int32 maxLODThreshold;
            public Int32 assignedLOD;
            public bool isPlayer;
            public bool isCulled;
            public Int32[] weights;
        };

        public class PropertyList
        {
            public PropertyList(ovrAvatar.Arbiter.AvailableProperties packet)
            {
                for (Int32 index = 0; index < packet.PropertiesLength; index++)
                {
                    var prop = packet.Properties(index);
                    properties.Add(ArbiterHelpers.MakePropertyFromPacket(prop.Value));
                }
            }

            public readonly List<ArbiterBaseProperty> properties = new List<ArbiterBaseProperty>();
        }

        public class PropertyUpdate
        {
            public PropertyUpdate(ovrAvatar.Arbiter.PropertyUpdate packet)
            {
                property = ArbiterHelpers.MakePropertyFromPacket(packet);
            }

            public readonly ArbiterBaseProperty property;
        }

        public class NetworkStats
        {
            public NetworkStats(ovrAvatar.Arbiter.NetworkStatsPacket packet)
            {
                period = packet.Period;
                downloadTotalBytes = packet.DownloadTotalBytes;
                downloadSpeed = packet.DownloadSpeed;
                totalRequests = packet.TotalRequests;
                activeRequests = packet.ActiveRequests;

            }

            public float period;
            public Int64 downloadTotalBytes;
            public Int64 downloadSpeed;
            public Int64 totalRequests;
            public Int64 activeRequests;
        }

        public class MemoryStats
        {
            public MemoryStats(ovrAvatar.Arbiter.MemoryStatsPacket packet)
            {
                period = packet.Period;
                currBytesUsed = packet.CurrBytesUsed;
                currAllocationCount = packet.CurrAllocationCount;
                maxBytesUsed = packet.MaxBytesUsed;
                maxAllocationCount = packet.MaxAllocationCount;
                totalBytesUsed = packet.TotalBytesUsed;
                totalAllocationCount = packet.TotalAllocationCount;
            }

            public float period;
            public Int64 currBytesUsed;
            public Int64 currAllocationCount;
            public Int64 maxBytesUsed;
            public Int64 maxAllocationCount;
            public Int64 totalBytesUsed;
            public Int64 totalAllocationCount;
        }

        public class TaskStats
        {
            public TaskStats(ovrAvatar.Arbiter.TaskStatsPacket packet)
            {
                period = packet.Period;
                histogram = packet.GetHistogramArray();
                pendingTasks = packet.Pending;

            }
            public float period;
            public UInt32[] histogram;
            public Int32 pendingTasks;

        }

        public class Event
        {
            public Event(UInt64 timestamp, ovrAvatar.Arbiter.AvatarSDKEventPacket ev)
            {
                name = ev.Name;
                arrival = new DateTime(1970, 1, 1).AddMilliseconds(timestamp / 1000.0);
            }
            public string name;
            public DateTime arrival;
        }

    }

#pragma warning restore 0649

    public class ArbiterHelpers
    {
        public static string TimeFormat()
        {
            return DateTime.Now.ToString("hh:mm:ss: ");
        }



        public static string ToString(ovrAvatar.Arbiter.PropertyUpdate propertyUpdatePacket)
        {
            switch (propertyUpdatePacket.ValueType)
            {
                case ovrAvatar.Arbiter.PropertyValue.IntegerValue:
                    return propertyUpdatePacket.ValueAsIntegerValue().I.ToString();
                case ovrAvatar.Arbiter.PropertyValue.LongValue:
                    return propertyUpdatePacket.ValueAsLongValue().L.ToString();
                case ovrAvatar.Arbiter.PropertyValue.StringValue:
                    return propertyUpdatePacket.ValueAsStringValue().S;
                case ovrAvatar.Arbiter.PropertyValue.FloatValue:
                    return propertyUpdatePacket.ValueAsFloatValue().F.ToString();
                case ovrAvatar.Arbiter.PropertyValue.BoolValue:
                    return propertyUpdatePacket.ValueAsBoolValue().B.ToString();
                case ovrAvatar.Arbiter.PropertyValue.Vec3Value:
                    var vec3 = propertyUpdatePacket.ValueAsVec3Value();
                    return $"( {vec3.X}, {vec3.Y}, {vec3.Z} )";
                case ovrAvatar.Arbiter.PropertyValue.Vec4Value:
                    var vec4 = propertyUpdatePacket.ValueAsVec4Value();
                    return $"( {vec4.X}, {vec4.Y}, {vec4.Z}, {vec4.W} )";
                case ovrAvatar.Arbiter.PropertyValue.QuatValue:
                    var quat = propertyUpdatePacket.ValueAsQuatValue();
                    return $"( {quat.X}, {quat.Y}, {quat.Z}, {quat.W} )";
                case ovrAvatar.Arbiter.PropertyValue.JointParamsValue:
                    {
                        var jp = propertyUpdatePacket.ValueAsJointParamsValue();
                        StringBuilder sb = new StringBuilder();
                        sb.Append($"( (r {jp.Rotation.Value.X}, {jp.Rotation.Value.Y}, {jp.Rotation.Value.Z}, {jp.Rotation.Value.W} ), ");
                        sb.Append($"(t {jp.Translation.Value.X}, {jp.Translation.Value.Y}, {jp.Translation.Value.Z} ), ");
                        sb.Append($"(s {jp.Scale.Value.X}, {jp.Scale.Value.Y}, {jp.Scale.Value.Z} ) )");
                        return sb.ToString();
                    }
                case ovrAvatar.Arbiter.PropertyValue.LODValue:
                    {
                        var lr = propertyUpdatePacket.ValueAsLODValue();
                        StringBuilder sb = new StringBuilder();
                        sb.Append($"( id {lr.Id}, importance {lr.ImportanceScore}, max LOD {lr.MaxLodThreshold}, assigned LOD {lr.AssignedLod}, ");
                        sb.Append($" player {lr.IsPlayer}, culled {lr.IsCulled}, weights: (");
                        for (Int32 index = 0; index < lr.WeightsLength; index++)
                        {
                            sb.Append($"{index}: {lr.Weights(index)}");
                            if (index != lr.WeightsLength - 1)
                            {
                                sb.Append(", ");
                            }
                        }
                        sb.Append(") )");
                        return sb.ToString();
                    }

                default: return "<Unknown>";
            }
        }

        public static ArbiterBaseProperty MakePropertyFromPacket(ovrAvatar.Arbiter.PropertyUpdate entry)
        {
            var id = entry.Id;
            switch (entry.ValueType)
            {
                case ovrAvatar.Arbiter.PropertyValue.IntegerValue:
                    return new ArbiterIntProperty(id, entry.ValueAsIntegerValue().I);
                case ovrAvatar.Arbiter.PropertyValue.LongValue:
                    return new ArbiterLongProperty(id, entry.ValueAsLongValue().L);
                case ovrAvatar.Arbiter.PropertyValue.StringValue:
                    return new ArbiterStringProperty(id, entry.ValueAsStringValue().S);
                case ovrAvatar.Arbiter.PropertyValue.BoolValue:
                    return new ArbiterBoolProperty(id, entry.ValueAsBoolValue().B);
                case ovrAvatar.Arbiter.PropertyValue.FloatValue:
                    return new ArbiterFloatProperty(id, entry.ValueAsFloatValue().F);
                case ovrAvatar.Arbiter.PropertyValue.Vec3Value:
                    {
                        var received = entry.ValueAsVec3Value();
                        var vecValue = new ArbiterStructs.Vec3
                        {
                            x = received.X,
                            y = received.Y,
                            z = received.Z
                        };
                        return new ArbiterVec3Property(id, vecValue);
                    }
                case ovrAvatar.Arbiter.PropertyValue.Vec4Value:
                    {
                        var received = entry.ValueAsVec4Value();
                        var vecValue = new ArbiterStructs.Vec4
                        {
                            x = received.X,
                            y = received.Y,
                            z = received.Z,
                            w = received.W
                        };
                        return new ArbiterVec4Property(id, vecValue);
                    }
                case ovrAvatar.Arbiter.PropertyValue.QuatValue:
                    {
                        var received = entry.ValueAsQuatValue();
                        var quatValue = new ArbiterStructs.Quat
                        {
                            x = received.X,
                            y = received.Y,
                            z = received.Z,
                            w = received.W
                        };
                        return new ArbiterQuatProperty(id, quatValue);
                    }

                case ovrAvatar.Arbiter.PropertyValue.JointParamsValue:
                    {
                        var received = entry.ValueAsJointParamsValue();
                        var trans = new ArbiterStructs.Vec3
                        {
                            x = received.Translation.Value.X,
                            y = received.Translation.Value.Y,
                            z = received.Translation.Value.Z
                        };
                        var scale = new ArbiterStructs.Vec3
                        {
                            x = received.Scale.Value.X,
                            y = received.Scale.Value.Y,
                            z = received.Scale.Value.Z
                        };

                        var rot = new ArbiterStructs.Quat
                        {
                            x = received.Rotation.Value.X,
                            y = received.Rotation.Value.Y,
                            z = received.Rotation.Value.Z,
                            w = received.Rotation.Value.W
                        };
                        var jpValue = new ArbiterStructs.JointParams
                        {
                            translation = trans,
                            rotation = rot,
                            scale = scale
                        };
                        return new ArbiterJointParamsProperty(id, jpValue);
                    }
                case ovrAvatar.Arbiter.PropertyValue.LODValue:
                    {
                        var received = entry.ValueAsLODValue();
                        var lodValue = new ArbiterStructs.LODRecord();
                        lodValue.id = received.Id;
                        lodValue.importanceScore = received.ImportanceScore;
                        lodValue.maxLODThreshold = received.MaxLodThreshold;
                        lodValue.isPlayer = received.IsPlayer;
                        lodValue.weights = received.GetWeightsArray();
                        return new ArbiterLODRecordProperty(id, lodValue);
                    }
                default:
                    return null;
            }
        }
        public static ArbiterBaseProperty MakePropertyFromValue<T>(string tag, T inValue)
        {
            switch (inValue)
            {
                case bool b:
                    return new ArbiterBoolProperty(tag, b);
                case Int32 i:
                    return new ArbiterIntProperty(tag, i);
                case Int64 l:
                    return new ArbiterLongProperty(tag, l);
                case float f:
                    return new ArbiterFloatProperty(tag, f);
                case string s:
                    return new ArbiterStringProperty(tag, s);
                case ArbiterStructs.Vec3 v3:
                    return new ArbiterVec3Property(tag, v3);
                case ArbiterStructs.Vec4 v4:
                    return new ArbiterVec4Property(tag, v4);
                case ArbiterStructs.Quat q:
                    return new ArbiterQuatProperty(tag, q);
                case ArbiterStructs.JointParams jp:
                    return new ArbiterJointParamsProperty(tag, jp);
                case ArbiterStructs.LODRecord lr:
                    return new ArbiterLODRecordProperty(tag, lr);
                default:
                    return null;
            }
        }

        public static UInt64 MicrosecondsSinceEpochUTC()
        {
            DateTime unixEpoch = new DateTime(1970, 1, 1);
            DateTime now = DateTime.UtcNow;

            TimeSpan ts = now.Subtract(unixEpoch);
            double utcMillisecondsSinceEpoch = ts.TotalMilliseconds;

            return (UInt64)Math.Round(utcMillisecondsSinceEpoch * 1000);
        }
    }

    // ABC for a property whose updates are supported by the Arbiter protocol
    // Subsequent classes implement specifics for int, long, float, bool, string,
    // and various bigger classes supported by Arbiter (vec, quat, joint params, lod record).

    public abstract class ArbiterBaseProperty
    {
        public ArbiterBaseProperty(string tag)
        {
            tag_ = tag;
            registered_ = false;
        }

        public string Tag()
        {
            return tag_;
        }

        public abstract Int32 TypeHash();
        public abstract bool IsBool();
        public abstract bool IsInteger();
        public abstract bool IsLong();
        public abstract bool IsFloat();
        public abstract bool IsString();
        public abstract bool IsVec3();
        public abstract bool IsVec4();
        public abstract bool IsQuat();
        public abstract bool IsJointParams();
        public abstract bool IsLODRecord();
        public abstract Int32 DataSize();

        public abstract ovrAvatar.Arbiter.PropertyValue ValueType();
        public abstract Int32 AddValue(FlatBufferBuilder fbb);

        public bool Registered()
        {
            return registered_;
        }

        public void MarkRegistered()
        {
            registered_ = true;
        }

        public abstract bool Update(ArbiterBaseProperty newValue);

        private string tag_;
        private bool registered_; // Whether we should report changes to remote subscriber
    };

    public abstract class ArbiterProperty<T> : ArbiterBaseProperty
    {
        public ArbiterProperty(string tag, T value) : base(tag)
        {
            value_ = value;
        }
        public override Int32 TypeHash()
        {
            return typeof(T).GetHashCode();
        }
        public T Value()
        {
            return value_;
        }

        public override Int32 DataSize()
        {
            return Marshal.SizeOf(value_);
        }


        public override bool Update(ArbiterBaseProperty newValue)
        {
            if (ValueType() != newValue.ValueType())
            {
                return false;
            }

            var newValueT = (ArbiterProperty<T>)newValue;
            value_ = newValueT.value_;

            return true;
        }

        public bool Update(T newValue)
        {
            value_ = newValue;
            return true;
        }

        public override bool IsInteger() { return value_.GetType() == typeof(Int32); }
        public override bool IsLong() { return value_.GetType() == typeof(Int64); }
        public override bool IsBool() { return value_.GetType() == typeof(bool); }
        public override bool IsFloat() { return value_.GetType() == typeof(float); }
        public override bool IsString() { return value_.GetType() == typeof(string); }
        public override bool IsVec3() { return value_.GetType() == typeof(ArbiterStructs.Vec3); }
        public override bool IsVec4() { return value_.GetType() == typeof(ArbiterStructs.Vec4); }
        public override bool IsQuat() { return value_.GetType() == typeof(ArbiterStructs.Quat); }
        public override bool IsJointParams() { return value_.GetType() == typeof(ArbiterStructs.JointParams); }
        public override bool IsLODRecord() { return value_.GetType() == typeof(ArbiterStructs.LODRecord); }

        protected T value_; // The value we carry
    };

    // We can take care of many ToString() implementations by doing all the scalar ones here.
    public abstract class ArbiterScalarProperty<T> : ArbiterProperty<T>
    {
        public ArbiterScalarProperty(string tag, T value) : base(tag, value) { }

        // All of the scalar value properties can share this method.
        public override string ToString()
        {
            return value_.ToString();
        }
    };


    public class ArbiterFloatProperty : ArbiterScalarProperty<float>
    {
        public ArbiterFloatProperty(string tag, float value) : base(tag, value) { }

        public override ovrAvatar.Arbiter.PropertyValue ValueType()
        {
            return ovrAvatar.Arbiter.PropertyValue.FloatValue;
        }

        public override Int32 AddValue(FlatBufferBuilder fbb)
        {
            return ovrAvatar.Arbiter.FloatValue.CreateFloatValue(fbb, value_).Value;
        }
    };

    class ArbiterIntProperty : ArbiterScalarProperty<Int32>
    {
        public ArbiterIntProperty(string tag, Int32 value) : base(tag, value) { }
        public override ovrAvatar.Arbiter.PropertyValue ValueType()
        {
            return ovrAvatar.Arbiter.PropertyValue.IntegerValue;
        }
        public override Int32 AddValue(FlatBufferBuilder fbb)
        {
            return ovrAvatar.Arbiter.IntegerValue.CreateIntegerValue(fbb, value_).Value;
        }
    };

    class ArbiterLongProperty : ArbiterScalarProperty<Int64>
    {
        public ArbiterLongProperty(string tag, Int64 value) : base(tag, value) { }

        public override ovrAvatar.Arbiter.PropertyValue ValueType()
        {
            return ovrAvatar.Arbiter.PropertyValue.LongValue;
        }
        public override Int32 AddValue(FlatBufferBuilder fbb)
        {
            return ovrAvatar.Arbiter.LongValue.CreateLongValue(fbb, value_).Value;
        }
    };

    class ArbiterBoolProperty : ArbiterScalarProperty<bool>
    {
        public ArbiterBoolProperty(string tag, bool value) : base(tag, value) { }

        public override ovrAvatar.Arbiter.PropertyValue ValueType()
        {
            return ovrAvatar.Arbiter.PropertyValue.BoolValue;
        }

        public override Int32 AddValue(FlatBufferBuilder fbb)
        {
            return ovrAvatar.Arbiter.BoolValue.CreateBoolValue(fbb, value_).Value;
        }
    }

    // Struct, struct of structs, and class types
    class ArbiterStringProperty : ArbiterProperty<string>
    {
        public ArbiterStringProperty(string tag, string value) : base(tag, value) { }

        public override ovrAvatar.Arbiter.PropertyValue ValueType()
        {
            return ovrAvatar.Arbiter.PropertyValue.StringValue;
        }

        public override Int32 AddValue(FlatBufferBuilder fbb)
        {
            var sloc = fbb.CreateString(value_);
            return ovrAvatar.Arbiter.StringValue.CreateStringValue(fbb, sloc).Value;
        }

        public override string ToString()
        {
            return value_;
        }
    };

    class ArbiterVec3Property : ArbiterProperty<ArbiterStructs.Vec3>
    {
        public ArbiterVec3Property(string tag, ArbiterStructs.Vec3 value) : base(tag, value) { }

        public override string ToString()
        {
            return $"{value_.x}, { value_.y}, {value_.z}";
        }

        public override ovrAvatar.Arbiter.PropertyValue ValueType()
        {
            return ovrAvatar.Arbiter.PropertyValue.Vec3Value;
        }

        public override Int32 AddValue(FlatBufferBuilder fbb)
        {
            return ovrAvatar.Arbiter.Vec3Value.CreateVec3Value(fbb, value_.x, value_.y, value_.z).Value;
        }
    };

    class ArbiterVec4Property : ArbiterProperty<ArbiterStructs.Vec4>
    {
        public ArbiterVec4Property(string tag, ArbiterStructs.Vec4 value) : base(tag, value) { }

        public override string ToString()
        {
            return $"{value_.x}, { value_.y}, {value_.z}, {value_.w}";
        }
        public override ovrAvatar.Arbiter.PropertyValue ValueType()
        {
            return ovrAvatar.Arbiter.PropertyValue.Vec4Value;
        }

        public override Int32 AddValue(FlatBufferBuilder fbb)
        {
            return ovrAvatar.Arbiter.Vec4Value.CreateVec4Value(fbb, value_.x, value_.y, value_.z, value_.z).Value;
        }
    };

    class ArbiterQuatProperty : ArbiterProperty<ArbiterStructs.Quat>
    {
        public ArbiterQuatProperty(string tag, ArbiterStructs.Quat value) : base(tag, value) { }

        public override string ToString()
        {
            return $"{value_.x}, { value_.y}, {value_.z}, , {value_.w}";
        }
        public override ovrAvatar.Arbiter.PropertyValue ValueType()
        {
            return ovrAvatar.Arbiter.PropertyValue.QuatValue;
        }

        public override Int32 AddValue(FlatBufferBuilder fbb)
        {
            return ovrAvatar.Arbiter.QuatValue.CreateQuatValue(fbb, value_.x, value_.y, value_.z, value_.z).Value;
        }
    };

    class ArbiterJointParamsProperty : ArbiterProperty<ArbiterStructs.JointParams>
    {
        public ArbiterJointParamsProperty(string tag, ArbiterStructs.JointParams value) : base(tag, value) { }

        public override string ToString()
        {
            string result;
            result = $"(t {value_.translation.x}, {value_.translation.y}, { value_.translation.z}),";
            result += $"(r {value_.rotation.x}, {value_.rotation.y}, {value_.rotation.z}, {value_.rotation.w}),";
            result += $"(s {value_.scale.x}, {value_.scale.y}, {value_.scale.z})";
            return result;
        }
        public override ovrAvatar.Arbiter.PropertyValue ValueType()
        {
            return ovrAvatar.Arbiter.PropertyValue.JointParamsValue;
        }

        public override Int32 AddValue(FlatBufferBuilder fbb)
        {
            var tloc = ovrAvatar.Arbiter.Vec3Value.CreateVec3Value(fbb, value_.translation.x, value_.translation.y, value_.translation.z);
            var sloc = ovrAvatar.Arbiter.Vec3Value.CreateVec3Value(fbb, value_.scale.x, value_.scale.y, value_.scale.z);
            var qloc = ovrAvatar.Arbiter.QuatValue.CreateQuatValue(fbb, value_.rotation.x, value_.rotation.y, value_.rotation.z, value_.rotation.w);

            return ovrAvatar.Arbiter.JointParamsValue.CreateJointParamsValue(fbb, qloc, tloc, sloc).Value;
        }
    };

    class ArbiterLODRecordProperty : ArbiterProperty<ArbiterStructs.LODRecord>
    {
        public ArbiterLODRecordProperty(string tag, ArbiterStructs.LODRecord value) : base(tag, value) { }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append($"( id {value_.id}, importance {value_.importanceScore}, max LOD {value_.maxLODThreshold}, assigned LOD {value_.assignedLOD}, ");
            sb.Append($" player {value_.isPlayer}, culled {value_.isCulled}, weights: (");
            for (Int32 index = 0; index < value_.weights.Length; index++)
            {
                sb.Append($"{index}: {value_.weights[index]}");
                if (index != value_.weights.Length - 1)
                {
                    sb.Append(", ");
                }
            }
            sb.Append(") )");
            return sb.ToString();

        }
        public override ovrAvatar.Arbiter.PropertyValue ValueType()
        {
            return ovrAvatar.Arbiter.PropertyValue.LODValue;
        }

        public override Int32 AddValue(FlatBufferBuilder fbb)
        {
            fbb.StartVector(Marshal.SizeOf(value_.weights[0]), value_.weights.Length, 0);
            foreach (Int32 weight in value_.weights)
            {
                fbb.AddInt(weight);
            }
            VectorOffset weightOffset = fbb.EndVector();

            return ovrAvatar.Arbiter.LODValue.CreateLODValue(fbb, value_.id, value_.importanceScore, value_.maxLODThreshold, value_.assignedLOD, value_.isPlayer, value_.isCulled, weightOffset).Value;
        }

    }
}
