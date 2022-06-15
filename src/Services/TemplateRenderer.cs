namespace Retweety.Services
{
    using System;
    using System.Collections.Generic;

    using HandlebarsDotNet;
    using HandlebarsDotNet.Helpers;

    using Retweety.Extensions;

    public static class TemplateRenderer
    {
        private static readonly IHandlebars _context;

        static TemplateRenderer()
        {
            _context = Handlebars.Create();
            _context.Configuration.TextEncoder = null;

            // Register helpers
            var helpers = GetHelpers();
            foreach (var (name, function) in helpers)
            {
                _context.RegisterHelper(name, function);
            }
            //_context.Configuration.
            HandlebarsHelpers.Register(_context);
        }

        public static string Parse(string text, dynamic model)
        {
            var template = _context.Compile(text ?? string.Empty);
            return template(model);
        }

        public static IReadOnlyDictionary<string, HandlebarsHelper> GetHelpers()
        {
            var dict = new Dictionary<string, HandlebarsHelper>
            {
                // Format boolean value helper
                ["formatBool"] = new HandlebarsHelper((writer, ctx, args) =>
                {
                    if (!bool.TryParse(args[0].ToString(), out var result))
                    {
                        writer.Write("No");
                        return;
                    }
                    writer.Write(result ? "Yes" : "No");
                }),
                ["formatPercentage"] = new HandlebarsHelper((writer, ctx, args) =>
                {
                    if (!double.TryParse(args[0].ToString(), out var percentage))
                        return;
                    var formatted = Math.Round(percentage * 100, 2);
                    writer.Write(formatted);
                }),
                ["isChecked"] = new HandlebarsHelper((writer, ctx, args) =>
                {
                    if (args[0] is string value)
                    {
                        var item = args[1].ToString();
                        var result = string.Equals(value, item, StringComparison.InvariantCultureIgnoreCase);
                        writer.Write(result ? "checked" : "");
                    }
                    else
                    {
                        writer.Write("");
                    }
                }),
                ["log"] = new HandlebarsHelper((writer, ctx, args) =>
                {
                    var json = args[0].ToJson();
                    Console.WriteLine($"hbs log: {json}");
                    writer.Write(json);
                }),
                ["gt"] = new HandlebarsHelper((writer, ctx, args) =>
                {
                    if (!int.TryParse(args[0].ToString(), out var arg1))
                        return;
                    if (!int.TryParse(args[1].ToString(), out var arg2))
                        return;
                    writer.Write(arg1 > arg2 ? "true" : "");
                }),
                ["lt"] = new HandlebarsHelper((writer, ctx, args) =>
                {
                    if (!int.TryParse(args[0].ToString(), out var arg1))
                        return;
                    if (!int.TryParse(args[1].ToString(), out var arg2))
                        return;
                    writer.Write(arg1 < arg2 ? "true" : "");
                }),
                ["gte"] = new HandlebarsHelper((writer, ctx, args) =>
                {
                    if (!int.TryParse(args[0].ToString(), out var arg1))
                        return;
                    if (!int.TryParse(args[1].ToString(), out var arg2))
                        return;
                    writer.Write(arg1 >= arg2 ? "true" : "");
                }),
                ["lte"] = new HandlebarsHelper((writer, ctx, args) =>
                {
                    if (!int.TryParse(args[0].ToString(), out var arg1))
                        return;
                    if (!int.TryParse(args[1].ToString(), out var arg2))
                        return;
                    writer.Write(arg1 <= arg2 ? "true" : "");

                }),
            };
            // TODO: Load helpers via file
            return dict;
        }
    }
}