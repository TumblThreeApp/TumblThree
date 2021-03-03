namespace TumblThree.Applications.Converter
{
    public static class PropertyCopier<TSrc, TDst> where TSrc : class
                                                where TDst : class
    {
        public static void Copy(TSrc src, TDst dst)
        {
            var srcProperties = src.GetType().GetProperties();
            var dstProperties = dst.GetType().GetProperties();

            foreach (var srcProperty in srcProperties)
            {
                foreach (var dstProperty in dstProperties)
                {
                    if (srcProperty.Name == dstProperty.Name && srcProperty.PropertyType == dstProperty.PropertyType)
                    {
                        dstProperty.SetValue(dst, srcProperty.GetValue(src));
                        break;
                    }
                }
            }
        }
    }
}
