﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using MediaTypeHeaderValue = System.Net.Http.Headers.MediaTypeHeaderValue;

namespace Discussion.Web.Tests
{
    public static class Extensions
    {
        
        public static T GetService<T>(this HttpContext httpContext) where T : class
        {
            return httpContext.RequestServices.GetService<T>();
        }
                
        public static T GetService<T>(this Controller controller) where T : class
        {
            return controller.HttpContext.RequestServices.GetService<T>();
        }

        
        public static string ReadAllContent(this HttpResponseMessage response)
        {
            return response.Content.ReadAsStringAsync().Result;
        }

        public static RequestBuilder WithCookie(this RequestBuilder request, Cookie cookie)
        {
            return WithCookie(request, cookie.Name, cookie.Value);
        }
        
        public static RequestBuilder WithCookie(this RequestBuilder request, string name, string value)
        {
            request.And(req =>
            {
                if (req.Headers.TryGetValues(HeaderNames.Cookie, out var existingCookies))
                {
                    req.Headers.Remove(HeaderNames.Cookie);
                    
                    var existing = existingCookies.First();
                    var cookieHeader = string.Concat(existing.TrimEnd(';', ' '), $"; {name}={value};");
                    req.Headers.Add(HeaderNames.Cookie, cookieHeader);
                }
                else
                {
                    req.Headers.Add(HeaderNames.Cookie, $"{name}={value};");
                }
            });
            return request;
        }
        
        public static RequestBuilder WithCookiesFrom(this RequestBuilder request, HttpResponseMessage response)
        {
            if (!response.Headers.TryGetValues(HeaderNames.SetCookie, out var cookies))
            {
                return request;
            }
            
            var responseCookieHeaders = SetCookieHeaderValue.ParseList(cookies.ToList()); 
            foreach (var cookie in responseCookieHeaders)
            {
                request.WithCookie(cookie.Name.ToString(), cookie.Value.ToString());
            }
            return request;
        }
        
        public static RequestBuilder WithJsonContent(this RequestBuilder request, object obj)
        {
            return request.And(req =>
            {
                var json = JsonConvert.SerializeObject(obj);
                req.Content = new StringContent(json);
                req.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json"); 
            });
        }
        
        public static RequestBuilder WithFormContent(this RequestBuilder request, Dictionary<string, string> obj)
        {
            return request.And(req =>
            {
                req.Content = new FormUrlEncodedContent(obj);
            });
        }

        public static RequestBuilder RequestAntiForgeryForm(this TestApplication app, string path, Dictionary<string, string> obj = null)
        {
            var tokens = app.GetAntiForgeryTokens();
            var request = app.Server.CreateRequest(path);
            return request.And(req =>
                {
                    if (obj == null)
                    {
                        obj = new Dictionary<string, string>();
                    }
                    
                    obj["__RequestVerificationToken"] = tokens.VerificationToken;
                    req.Content = new FormUrlEncodedContent(obj);
                })
                .WithCookie(tokens.Cookie);
        }



    }
}