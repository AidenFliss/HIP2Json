using System.IO;
using System.Text.Json.Serialization;

namespace HIP2Json;

public sealed class game_object_bungee_hookParser : AbstractDYNAParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart, short version, string dynaType)
    {
        return new game_object_bungee_hook
        {
            entity = ReadUInt32BE(br),
            enter = ReadVector3BE(br),
            attach_dist = ReadFloatBE(br),
            attach_travel_time = ReadFloatBE(br),
            detach_dist = ReadFloatBE(br),
            detach_free_fall_time = ReadFloatBE(br),
            detach_accel = ReadFloatBE(br),
            turn_unused0 = ReadFloatBE(br),
            turn_unused1 = ReadFloatBE(br),
            vertical_frequency = ReadFloatBE(br),
            vertical_gravity = ReadFloatBE(br),
            vertical_dive = ReadFloatBE(br),
            vertical_min_dist = ReadFloatBE(br),
            vertical_max_dist = ReadFloatBE(br),
            vertical_damp = ReadFloatBE(br),
            horizontal_max_dist = ReadFloatBE(br),
            camera_rest_dist = ReadFloatBE(br),
            camera_view_angle = ReadFloatBE(br),
            camera_offset = ReadFloatBE(br),
            camera_offset_dir = ReadFloatBE(br),
            camera_turn_speed = ReadFloatBE(br),
            camera_vel_scale = ReadFloatBE(br),
            camera_roll_speed = ReadFloatBE(br),
            camera_unused0 = ReadVector3BE(br),
            collision_hit_loss = ReadFloatBE(br),
            collision_damage_velocity = ReadFloatBE(br),
            collision_hit_velocity = ReadFloatBE(br)
        };
    }

    public override byte[] Serialize(object obj, short version, string dynaType)
    {
        game_object_bungee_hook bungeeHook = (game_object_bungee_hook)obj;
        
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        WriteUInt32BE(bw, bungeeHook.entity);
        WriteVector3BE(bw, bungeeHook.enter);
        WriteFloatBE(bw, bungeeHook.attach_dist);
        WriteFloatBE(bw, bungeeHook.attach_travel_time);
        WriteFloatBE(bw, bungeeHook.detach_dist);
        WriteFloatBE(bw, bungeeHook.detach_free_fall_time);
        WriteFloatBE(bw, bungeeHook.detach_accel);
        WriteFloatBE(bw, bungeeHook.turn_unused0); //diff in movie
        WriteFloatBE(bw, bungeeHook.turn_unused1);
        WriteFloatBE(bw, bungeeHook.vertical_frequency);
        WriteFloatBE(bw, bungeeHook.vertical_gravity);
        WriteFloatBE(bw, bungeeHook.vertical_dive);
        WriteFloatBE(bw, bungeeHook.vertical_min_dist);
        WriteFloatBE(bw, bungeeHook.vertical_max_dist);
        WriteFloatBE(bw, bungeeHook.vertical_damp);
        WriteFloatBE(bw, bungeeHook.horizontal_max_dist);
        WriteFloatBE(bw, bungeeHook.camera_rest_dist);
        WriteFloatBE(bw, bungeeHook.camera_view_angle);
        WriteFloatBE(bw, bungeeHook.camera_offset);
        WriteFloatBE(bw, bungeeHook.camera_offset_dir);
        WriteFloatBE(bw, bungeeHook.camera_turn_speed);
        WriteFloatBE(bw, bungeeHook.camera_vel_scale);
        WriteFloatBE(bw, bungeeHook.camera_roll_speed);
        WriteVector3BE(bw, bungeeHook.camera_unused0); //diff in movie
        WriteFloatBE(bw, bungeeHook.collision_hit_loss);
        WriteFloatBE(bw, bungeeHook.collision_damage_velocity);
        WriteFloatBE(bw, bungeeHook.collision_hit_velocity);

        return ms.ToArray();
    }

    public override string GetFolderName() { return "BungeeHook"; }
}

public class game_object_bungee_hook
{
    [JsonConverter(typeof(AssetIDConverter))]
    public uint entity { get; set; }
    public xVec3 enter { get; set; }
    public float attach_dist { get; set; }
    public float attach_travel_time { get; set; }
    public float detach_dist { get; set; }
    public float detach_free_fall_time { get; set; }
    public float detach_accel { get; set; }
    public float turn_unused0 { get; set; } //turn spring in movie
    public float turn_unused1 { get; set; } //turn delay in movie
    public float vertical_frequency { get; set; }
    public float vertical_gravity { get; set; }
    public float vertical_dive { get; set; }
    public float vertical_min_dist { get; set; }
    public float vertical_max_dist { get; set; }
    public float vertical_damp { get; set; }
    public float horizontal_max_dist { get; set; }
    public float camera_rest_dist { get; set; }
    public float camera_view_angle { get; set; }
    public float camera_offset { get; set; }
    public float camera_offset_dir { get; set; }
    public float camera_turn_speed { get; set; }
    public float camera_vel_scale { get; set; }
    public float camera_roll_speed { get; set; }
    public xVec3 camera_unused0 { get; set; } // camera speed in movie
    public float collision_hit_loss { get; set; }
    public float collision_damage_velocity { get; set; }
    public float collision_hit_velocity { get; set; }
}