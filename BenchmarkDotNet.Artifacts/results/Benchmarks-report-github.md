``` ini

BenchmarkDotNet=v0.13.5, OS=Windows 10 (10.0.19044.2728/21H2/November2021Update)
11th Gen Intel Core i7-1165G7 2.80GHz, 1 CPU, 8 logical and 4 physical cores
.NET SDK=7.0.202
  [Host]     : .NET 7.0.4 (7.0.423.11508), X64 RyuJIT AVX2
  DefaultJob : .NET 7.0.4 (7.0.423.11508), X64 RyuJIT AVX2


```
|                        Method |                input |             Mean |          Error |         StdDev |           Median |
|------------------------------ |--------------------- |-----------------:|---------------:|---------------:|-----------------:|
| **Span_NewStringBuilderEachTime** | **N&amp;&amp;#o(...)32789 [42]** |        **201.41 ns** |       **3.122 ns** |       **2.920 ns** |        **202.05 ns** |
| Span_ThreadLocalStringBuilder | N&amp;&amp;#o(...)32789 [42] |        119.57 ns |       1.205 ns |       1.128 ns |        119.74 ns |
|               Regex_NoCaching | N&amp;&amp;#o(...)32789 [42] |      2,639.63 ns |      52.908 ns |     101.936 ns |      2,677.36 ns |
|                 Regex_Caching | N&amp;&amp;#o(...)32789 [42] |      2,651.17 ns |      52.788 ns |     150.607 ns |      2,691.36 ns |
|                Regex_Compiled | N&amp;&amp;#o(...)32789 [42] |        614.55 ns |      12.266 ns |      23.338 ns |        619.33 ns |
| **Span_NewStringBuilderEachTime** |  **long(...)hing [220]** |        **914.87 ns** |      **13.853 ns** |      **17.013 ns** |        **908.25 ns** |
| Span_ThreadLocalStringBuilder |  long(...)hing [220] |        666.60 ns |       3.770 ns |       3.148 ns |        666.00 ns |
|               Regex_NoCaching |  long(...)hing [220] |     19,789.11 ns |     365.263 ns |     589.832 ns |     19,957.87 ns |
|                 Regex_Caching |  long(...)hing [220] |     20,097.20 ns |     402.248 ns |     714.996 ns |     20,372.37 ns |
|                Regex_Compiled |  long(...)hing [220] |      7,948.02 ns |     155.671 ns |     166.567 ns |      7,951.06 ns |
| **Span_NewStringBuilderEachTime** | **long(...)hing [2020]** |      **7,077.14 ns** |     **140.212 ns** |     **143.988 ns** |      **7,005.09 ns** |
| Span_ThreadLocalStringBuilder | long(...)hing [2020] |      5,543.05 ns |     109.063 ns |     185.197 ns |      5,447.32 ns |
|               Regex_NoCaching | long(...)hing [2020] |    733,925.18 ns |  10,269.502 ns |  13,353.253 ns |    736,096.48 ns |
|                 Regex_Caching | long(...)hing [2020] |    735,524.11 ns |   9,335.395 ns |   8,275.584 ns |    735,441.89 ns |
|                Regex_Compiled | long(...)hing [2020] |    312,207.90 ns |   3,953.773 ns |   3,504.916 ns |    312,759.86 ns |
| **Span_NewStringBuilderEachTime** |  **lon(...)ing [20020]** |     **57,038.57 ns** |   **1,134.866 ns** |   **3,237.837 ns** |     **56,156.52 ns** |
| Span_ThreadLocalStringBuilder |  lon(...)ing [20020] |     48,994.82 ns |     663.824 ns |     554.323 ns |     48,858.32 ns |
|               Regex_NoCaching |  lon(...)ing [20020] | 61,531,497.62 ns | 656,067.901 ns | 581,587.106 ns | 61,457,600.00 ns |
|                 Regex_Caching |  lon(...)ing [20020] | 60,840,731.73 ns | 748,975.723 ns | 625,428.650 ns | 60,711,100.00 ns |
|                Regex_Compiled |  lon(...)ing [20020] | 26,380,720.54 ns | 145,252.987 ns | 128,762.990 ns | 26,391,782.81 ns |
| **Span_NewStringBuilderEachTime** | **simpleCamelCaseThing** |        **122.47 ns** |       **2.500 ns** |       **4.875 ns** |        **121.73 ns** |
| Span_ThreadLocalStringBuilder | simpleCamelCaseThing |         85.92 ns |       1.403 ns |       1.312 ns |         86.06 ns |
|               Regex_NoCaching | simpleCamelCaseThing |      1,099.74 ns |      21.626 ns |      42.180 ns |      1,106.97 ns |
|                 Regex_Caching | simpleCamelCaseThing |      1,100.75 ns |      21.353 ns |      23.733 ns |      1,105.85 ns |
|                Regex_Compiled | simpleCamelCaseThing |        479.22 ns |       9.607 ns |      16.825 ns |        481.27 ns |
| **Span_NewStringBuilderEachTime** |  **рашенКамелКейсThing** |        **308.56 ns** |       **1.568 ns** |       **1.390 ns** |        **308.46 ns** |
| Span_ThreadLocalStringBuilder |  рашенКамелКейсThing |        263.12 ns |       4.955 ns |       7.714 ns |        259.83 ns |
|               Regex_NoCaching |  рашенКамелКейсThing |        590.95 ns |       9.923 ns |       8.797 ns |        593.35 ns |
|                 Regex_Caching |  рашенКамелКейсThing |        573.81 ns |       7.595 ns |       6.733 ns |        574.15 ns |
|                Regex_Compiled |  рашенКамелКейсThing |        352.66 ns |       1.855 ns |       1.448 ns |        352.56 ns |
