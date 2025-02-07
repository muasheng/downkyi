﻿using Brotli;
using DownKyi.Core.BiliApi.Login;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;

namespace DownKyi.Core.BiliApi
{
    internal static class WebClient
    {

        /// <summary>
        /// 发送get或post请求
        /// </summary>
        /// <param name="url"></param>
        /// <param name="referer"></param>
        /// <param name="method"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static string RequestWeb(string url, string referer = null, string method = "GET", Dictionary<string, string> parameters = null, int retry = 3)
        {
            // 重试次数
            if (retry <= 0) { return ""; }

            // post请求，发送参数
            if (method == "POST" && parameters != null)
            {
                StringBuilder builder = new StringBuilder();
                int i = 0;
                foreach (var item in parameters)
                {
                    if (i > 0)
                    {
                        builder.Append("&");
                    }

                    builder.AppendFormat("{0}={1}", item.Key, item.Value);
                    i++;
                }

                url += "?" + builder.ToString();
            }

            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = method;
                request.Timeout = 30 * 1000;

                // MacOS Safari
                //string safari = "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_5) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/13.1.1 Safari/605.1.15";
                // Windows 10 Chrome
                string chrome = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/97.0.4692.99 Safari/537.36";
                request.UserAgent = chrome;

                //request.ContentType = "application/json,text/html,application/xhtml+xml,application/xml;charset=UTF-8";
                request.Headers["accept-language"] = "zh-CN,zh;q=0.9,en-US;q=0.8,en;q=0.7";
                request.Headers["accept-encoding"] = "gzip, deflate, br";

                // referer
                if (referer != null)
                {
                    request.Referer = referer;
                }

                // 构造cookie
                if (!url.Contains("getLogin"))
                {
                    request.Headers["origin"] = "https://www.bilibili.com";

                    CookieContainer cookies = LoginHelper.GetLoginInfoCookies();
                    if (cookies != null)
                    {
                        request.CookieContainer = cookies;
                    }
                }

                string html = string.Empty;
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    if (response.ContentEncoding.ToLower().Contains("gzip"))
                    {
                        using (GZipStream stream = new GZipStream(response.GetResponseStream(), CompressionMode.Decompress))
                        {
                            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                            {
                                html = reader.ReadToEnd();
                            }
                        }
                    }
                    else if (response.ContentEncoding.ToLower().Contains("deflate"))
                    {
                        using (DeflateStream stream = new DeflateStream(response.GetResponseStream(), CompressionMode.Decompress))
                        {
                            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                            {
                                html = reader.ReadToEnd();
                            }
                        }
                    }
                    else if (response.ContentEncoding.ToLower().Contains("br"))
                    {
                        using (BrotliStream stream = new BrotliStream(response.GetResponseStream(), CompressionMode.Decompress))
                        {
                            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                            {
                                html = reader.ReadToEnd();
                            }
                        }
                    }
                    else
                    {
                        using (Stream stream = response.GetResponseStream())
                        {
                            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                            {
                                html = reader.ReadToEnd();
                            }
                        }
                    }
                }

                return html;
            }
            catch (WebException e)
            {
                Console.WriteLine("RequestWeb()发生Web异常: {0}", e);
                Logging.LogManager.Error(e);
                return RequestWeb(url, referer, method, parameters, retry - 1);
            }
            catch (IOException e)
            {
                Console.WriteLine("RequestWeb()发生IO异常: {0}", e);
                Logging.LogManager.Error(e);
                return RequestWeb(url, referer, method, parameters, retry - 1);
            }
            catch (Exception e)
            {
                Console.WriteLine("RequestWeb()发生其他异常: {0}", e);
                Logging.LogManager.Error(e);
                return RequestWeb(url, referer, method, parameters, retry - 1);
            }
        }
    }
}
