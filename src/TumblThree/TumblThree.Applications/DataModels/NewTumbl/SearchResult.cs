using System;
using System.Collections.Generic;

namespace TumblThree.Applications.DataModels.NewTumbl
{
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    public class AField
    {
        public string sName { get; set; }
        public object oMin { get; set; }
        public object oMax { get; set; }
        public string sType { get; set; }
        public bool bNumeric { get; set; }
    }

    public class AResultSet
    {
        public List<ARow> aRow { get; set; }
        public int? nTotalRows { get; set; }
        public List<AField> aField { get; set; }
    }

    public class ARow
    {
        public DateTime dtCreated { get; set; }
        public int dwUserIx { get; set; }
        public byte bVerified { get; set; }
        public short nBirthYear { get; set; }
        public byte bTOS { get; set; }
        public byte bStatus { get; set; }
        public long? qwPartIx { get; set; }
        public string szIPAddress { get; set; }
        public byte? bPartTypeIx { get; set; }
        public string szBody { get; set; }
        public string szSub { get; set; }
        public long? qwMediaIx { get; set; }
        public byte? bMediaTypeIx { get; set; }
        public short? nWidth { get; set; }
        public short? nHeight { get; set; }
        public int? nSize { get; set; }
        public int? nLength { get; set; }
        public int? dwBlogIx { get; set; }
        public string szBlogId { get; set; }
        public string szTitle { get; set; }
        public string szDescription { get; set; }
        public byte? bPrimary { get; set; }
        public byte? bIconShape { get; set; }
        public long? qwPartIx_Icon { get; set; }
        public long? qwPartIx_Banner { get; set; }
        public long? qwPartIx_Background { get; set; }
        public int? dwColor_Background { get; set; }
        public int? dwColor_Foreground { get; set; }
        public byte? bRatingIx { get; set; }
        public byte? bNoIndex { get; set; }
        public byte? bHide { get; set; }
        public byte? bFollow { get; set; }
        public byte? bLoggedIn { get; set; }
        public byte? bTerms { get; set; }
        public byte? bMinor { get; set; }
        public byte? bRating_Blogs { get; set; }
        public byte? bRating_BlogLinks { get; set; }
        public byte? bHome { get; set; }
        public byte? bSize_GIF { get; set; }
        public byte? bSize_Post { get; set; }
        public byte? bUnits { get; set; }
        public int? dwGeoCityIx { get; set; }
        public DateTime? dtProbation { get; set; }
        public int? nAdCat { get; set; }
        public string szHref { get; set; }
        public int? dwAdmin { get; set; }
        public byte? bActive { get; set; }
        public int? nCount_Blog_Message { get; set; }
        public int? nCount_Post_Ask { get; set; }
        public int? nCount_Post_Submit { get; set; }
        public int? nCount_Post_OutOfRange { get; set; }
        public int? nCount_Post_Flagged { get; set; }
        public DateTime? dtSearch { get; set; }
        public string szName { get; set; }
        public string acLanguage { get; set; }
        public string acCountry { get; set; }
        public string szLocation { get; set; }
        public byte? bAge { get; set; }
        public byte? bGender { get; set; }
        public byte? bOnline { get; set; }
        public byte? bGenreTypeIx { get; set; }
        public int? dwAtomIx_Subgenre { get; set; }
        public byte? bOrder { get; set; }
        public string szSubgenre { get; set; }

        // posts
        public long? qwPostIx { get; set; }
        public long? qwPostIx_Orig { get; set; }
        public int? dwBlogIx_Orig { get; set; }
        public long? qwPostIx_From { get; set; }
        public int? dwBlogIx_From { get; set; }
        public int? dwBlogIx_Submit { get; set; }
        public byte? bPostTypeIx { get; set; }
        public string szURL { get; set; }
        public string szSource { get; set; }
        public DateTime? dtActive { get; set; }
        public DateTime? dtScheduled { get; set; }
        public DateTime? dtModified { get; set; }
        public DateTime? dtDeleted { get; set; }
        public byte? nTierIz { get; set; }
        public byte? bState { get; set; }
        public long? dwChecksum { get; set; }
        public string szExternal { get; set; }
        public int? nCount_Post { get; set; }
        public int? nCount_Like { get; set; }
        public int? nCount_Comment { get; set; }
        public DateTime? dtLike { get; set; }
        public DateTime? dtFavorite { get; set; }
        public DateTime? dtFlag { get; set; }
        public byte? nCount_Mark { get; set; }
        public short? nPartIz { get; set; }
        public string szTagId { get; set; }
        // errors
        public int? dwError { get; set; }
        public string szError { get; set; }
    }

    public class Root
    {
        public string nResult { get; set; }
        public List<AResultSet> aResultSet { get; set; }

        public string sError { get; set; }

        public string sAPIErrorCode { get; set; }

        public string sAPIErrorMessage { get; set; }
    }
}
