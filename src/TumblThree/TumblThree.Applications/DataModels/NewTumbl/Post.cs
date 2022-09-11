using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace TumblThree.Applications.DataModels.NewTumbl
{
    public static class PostType
    {
        public static byte Text => 1;
        public static byte Quote => 2;
        public static byte Answer => 3;
        public static byte Link => 4;
        public static byte Photo => 5;
        public static byte Video => 7;
        public static byte Comment => 8;
        public static byte Audio => 99;
    }

    [DataContract]
    [JsonObject(MemberSerialization = MemberSerialization.OptOut)]
    public class Post
    {
        public DateTime dtCreated { get; set; }
        public long? qwPostIx { get; set; }
        public int? dwBlogIx { get; set; }
        public long? qwPostIx_Orig { get; set; }
        public int? dwBlogIx_Orig { get; set; }
        public long? qwPostIx_From { get; set; }
        public int? dwBlogIx_From { get; set; }
        public int? dwBlogIx_Submit { get; set; }
        public byte? bPostTypeIx { get; set; }
        public byte? bRatingIx { get; set; }
        public string szURL { get; set; }
        public string szSource { get; set; }
        public DateTime? dtActive { get; set; }
        public DateTime? dtScheduled { get; set; }
        public DateTime? dtModified { get; set; }
        public DateTime? dtDeleted { get; set; }
        public byte? nTierIz { get; set; }
        public byte? bState { get; set; }
        public byte bStatus { get; set; }
        public long? dwChecksum { get; set; }
        public string szExternal { get; set; }
        public int? nCount_Post { get; set; }
        public int? nCount_Like { get; set; }
        public int? nCount_Comment { get; set; }
        public DateTime? dtLike { get; set; }
        public DateTime? dtFavorite { get; set; }
        public DateTime? dtFlag { get; set; }
        public byte? nCount_Mark { get; set; }

        public List<Part> Parts { get; set; }
        public List<Tag> Tags { get; set; }

        public List<string> DownloadedUrls { get; private set; }

        public List<string> DownloadedFilenames { get; private set; }

        public Post()
        {
            DownloadedUrls = new List<string>();
            DownloadedFilenames = new List<string>();
        }

        public object Clone()
        {
            return MemberwiseClone();
        }

        public static Post Create(ARow post)
        {
            return JsonConvert.DeserializeObject<Post>(JsonConvert.SerializeObject(post));
        }
    }

    [DataContract]
    [JsonObject(MemberSerialization = MemberSerialization.OptOut)]
    public class Part
    {
        public DateTime? dtScheduled { get; set; }
        public long? qwPostIx { get; set; }
        public short? nPartIz { get; set; }
        public long? qwPostIx_From { get; set; }
        public int? dwBlogIx_From { get; set; }
        public byte? bOrder { get; set; }
        public byte? bPartTypeIx { get; set; }
        public long? qwPartIx { get; set; }

        public List<Media> Medias { get; set; }

        public static Part Create(ARow part)
        {
            return JsonConvert.DeserializeObject<Part>(JsonConvert.SerializeObject(part));
        }
    }

    [DataContract]
    [JsonObject(MemberSerialization = MemberSerialization.OptOut)]
    public class Media
    {
        public DateTime dtCreated { get; set; }
        public long? qwPartIx { get; set; }
        public string szIPAddress { get; set; }
        public int dwUserIx { get; set; }
        public byte? bPartTypeIx { get; set; }
        public string szBody { get; set; }
        public string szSub { get; set; }
        public long? qwMediaIx { get; set; }
        public byte? bMediaTypeIx { get; set; }
        public short? nWidth { get; set; }
        public short? nHeight { get; set; }
        public int? nSize { get; set; }
        public int? nLength { get; set; }
        public byte bStatus { get; set; }

        public static Media Create(ARow media)
        {
            return JsonConvert.DeserializeObject<Media>(JsonConvert.SerializeObject(media));
        }
    }

    [DataContract]
    [JsonObject(MemberSerialization = MemberSerialization.OptOut)]
    public class Tag
    {
        public long? qwPostIx { get; set; }
        public byte? bOrder { get; set; }
        public string szTagId { get; set; }

        public static Tag Create(ARow tag)
        {
            return new Tag() { qwPostIx = tag.qwPostIx, bOrder = tag.bOrder, szTagId = tag.szTagId };
        }
    }
}
