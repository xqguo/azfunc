namespace Company.Function

open System
open System.IO
open System.Globalization
open Microsoft.AspNetCore.Mvc
open Microsoft.Azure.WebJobs
open Microsoft.Azure.WebJobs.Extensions.Http
open Microsoft.AspNetCore.Http
open Newtonsoft.Json
open Microsoft.Extensions.Logging


module HttpTrigger =
    // Define a nullable container to deserialize into.
    [<AllowNullLiteral>]
    type NameContainer() =
        member val Name = "" with get, set

    // For convenience, it's better to have a central place for the literal.
    [<Literal>]
    let Name = "name"

    let cal = ChineseLunisolarCalendar()

    let JiaZi = [|
            "甲子"; "乙丑"; "丙寅"; "丁卯"; "戊辰"; "己巳"; "庚午"; "辛未"; "壬申"; "癸酉";
            "甲戊"; "乙亥"; "丙子"; "丁丑"; "戊寅"; "乙卯"; "庚辰"; "辛巳"; "壬午"; "癸未";    
            "甲申"; "乙酉"; "丙戌"; "丁亥"; "戊子"; "己丑"; "庚寅"; "辛卯"; "壬辰"; "癸巳";        
            "甲午"; "乙未"; "丙申"; "丁酉"; "戊戌"; "己亥"; "庚子"; "辛丑"; "壬寅"; "癸卯";        
            "甲辰"; "乙巳"; "丙午"; "丁未"; "戊申"; "乙酉"; "庚戌"; "辛亥"; "壬子"; "癸丑";         
            "甲寅"; "乙卯"; "丙辰"; "丁巳"; "戊午"; "己未"; "庚申"; "辛酉"; "壬戌"; "癸亥" |]

    let SolarTime = 
        [|"04-Feb-2022 4:51:00 AM";"19-Feb-2022 12:43:00 AM";"05-Mar-2022 10:44:00 PM";"20-Mar-2022 11:33:00 PM";"05-Apr-2022 3:20:00 AM";"20-Apr-2022 10:24:00 AM";"05-May-2022 8:26:00 PM";"21-May-2022 9:23:00 AM";"06-Jun-2022 12:26:00 AM";"21-Jun-2022 5:14:00 PM";"07-Jul-2022 10:38:00 AM";"23-Jul-2022 4:07:00 AM";"07-Aug-2022 8:29:00 PM";"23-Aug-2022 11:16:00 AM";"07-Sep-2022 11:32:00 PM";"23-Sep-2022 9:04:00 AM";"08-Oct-2022 3:22:00 PM";"23-Oct-2022 6:36:00 PM";"07-Nov-2022 6:45:00 PM";"22-Nov-2022 4:20:00 PM";"07-Dec-2022 11:46:00 AM";"22-Dec-2022 5:48:00 AM";"05-Jan-2023 11:05:00 PM";"20-Jan-2023 4:30:00 PM"|]
        |> Array.map( DateTime.Parse)
    let SolarTerm = [| "立春"; "雨水"; "惊蛰"; "春分"; "清明"; "谷雨"; "立夏"; "小满"; "芒种"; "夏至"; "小暑"; "大暑"; "立秋"; "处暑"; "白露"; "秋分"; "寒露"; "霜降"; "立冬"; "小雪"; "大雪"; "冬至" ;"小寒"; "大寒" |]

    let getYearStart (d:DateTime) = 
        let mm = 365.2421990741*24.*60.  // let mm = 525948.76
        let y = d.Year
        let num = int ( mm * float (y - 2022 ))
        let dd = SolarTime[0].AddMinutes(num)
        if dd.Date <= d.Date then 
            dd 
        else 
            dd.AddMinutes(-mm)
            
    let timefromYearStart = 
        SolarTime
        |> Array.map( fun d -> d - SolarTime[0])

    let getJieQi (d:DateTime) = 
        let s = getYearStart d 
        timefromYearStart 
        |> Array.mapi( fun i t ->
            let d = s + t 
            d.Date,SolarTerm[i])

    let getYearMonth (d:DateTime) =
        let yr = ( ((getYearStart d).Year - 1984 ) % 60 + 60) % 60 // let yr = cal.GetSexagenaryYear (getYearStart d) - 1
        let mth = (yr % 5 ) * 12 + 2
        getJieQi d 
        |> Array.splitInto 12 
        |> Array.map( Array.head)
        |> Array.mapi( fun i d -> fst d, JiaZi[yr], JiaZi[(i+mth) % 60])
        
    let getDayTime d = 
        let ganzhiStartDay = DateTime(1899, 12, 22) //起始日: 甲子 甲子
        let ri = JiaZi.[(int (d - ganzhiStartDay).TotalDays) % 60]
        let hr = JiaZi.[(int ((d - ganzhiStartDay.AddHours(-1.0)).TotalHours / 2.0 )) % 60]
        ri,hr

    let getYearMonthDayTime d =
        let (dd,tt) = getDayTime d
        let (_,yy,mm) = getYearMonth d |> Array.findBack( fun (x,_,_) -> x <= d )
        yy,mm,dd,tt
        
    let getLunarYearMonthDay (d:DateTime) =
        let yr = (d.AddDays -(float <| cal.GetDayOfYear d)).Year
        let mm = cal.GetMonth d
        let dd = cal.GetDayOfMonth d 
        let l = cal.GetLeapMonth yr
        let mm' = 
            match l with
            | 0 -> mm
            | n when n = mm -> -mm + 1
            | n when n < mm -> mm - 1 
            | _ -> mm
        yr,mm',dd
        
    let getFull d =
        let yy,m,x = getLunarYearMonthDay d
        let yys = JiaZi[((yy - 1984 ) % 60 + 60) % 60] // let yr = cal.GetSexagenaryYear (getYearStart d) - 1
        let n,y,r,s = getYearMonthDayTime d
        let HZNum = "零一二三四五六七八九"
        let nStr1 = "日一二三四五六七八九"
        let nStr2 = "初十廿卅"
        let monthString = [| "正月";"二月";"三月";"四月";"五月";"六月";"七月";"八月";"九月";"十月";"十一月";"腊月" |]
        let ms = (if m < 0 then "闰" else "" ) + monthString[abs m - 1]
        let ds = String.Concat [|  nStr2[x/10];nStr1[x%10] |]
        let jq = 
            getJieQi d
            |> Array.tryFind( fun x -> (fst x).Date = d.Date ) 
            |> function | Some x -> snd x | None -> ""
        let ot = 
            match (m,x) with
            | (1,1) -> "春节"
            | (1,15) -> "元宵"
            | (5,5) -> "端午"
            | (7,7) -> "七夕"
            | (7,15) -> "中元"
            | (8,15) -> "中秋"
            | (9,9) -> "重阳"
            | _ -> ""
        let ot' = 
            if cal.GetLeapMonth yy = 13 then //1574 and 3358
                match (m,x) with
                | (12,8) -> "腊八"
                | (12,23) -> "小年"
                | (12,24) -> "掸尘"
                | _ -> ""
            else 
                match (m,x) with
                | (-12,8) -> "腊八"
                | (-12,23) -> "小年"
                | (-12,24) -> "掸尘"
                | _ -> ""
        let ye = if (cal.GetDaysInYear yy ) = (cal.GetDayOfYear d) then "除夕" else ""  
        "农历" + yys + yy.ToString() + "年" + ms + ds + jq + ot + ot' + ye + n + y + r + s 

    let parseDateExact (format:string) str= 
        let (s,d) = DateTime.TryParseExact( str, format, CultureInfo.InvariantCulture, DateTimeStyles.None)
        if s then Some d else None

    let parseDate (str:string)= 
        let (s,d) = DateTime.TryParse(str)
        if s then Some d else None

    // active patterns for try-parsing strings
    let (|YYYY|_|)   = parseDateExact "yyyy"
    let (|YYYYMM|_|)   = parseDateExact "yyyyMM"
    let (|Date|_|)   = parseDate

    [<FunctionName("HttpTrigger")>]
    let run ([<HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)>]req: HttpRequest) (log: ILogger) =
        async {
            log.LogInformation("F# HTTP trigger function processed a request.")

            let str = 
                if req.Query.ContainsKey("date") then (req.Query.["date"].[0]) else ""
            let dates = 
                match str with
                |YYYY d -> Array.init 366 ( fun i -> d.AddDays( float i))|> Array.filter( fun v -> v < d.AddYears 1 )
                |YYYYMM d -> Array.init 31 ( fun i -> d.AddDays(float i)) |> Array.filter( fun v -> v < d.AddMonths 1 )
                |Date d -> [| d |]
                | _ -> 
                    [| TimeZoneInfo.ConvertTimeFromUtc( DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("China Standard Time")) |]
            
            let responseMessage =             
                dates |> Array.map( fun d -> $"{d} {getFull d}" )|> String.concat "\n"

            return OkObjectResult(responseMessage) :> IActionResult
        } |> Async.StartAsTask