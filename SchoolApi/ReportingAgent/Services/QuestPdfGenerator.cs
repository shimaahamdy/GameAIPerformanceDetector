using GameAi.Api.ReportingAgent.DTOs;
using GameAi.Api.ReportingAgent.Services.Contracts;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace GameAi.Api.ReportingAgent.Services
{
    public class QuestPdfGenerator : IPdfGenerator
    {
        public async Task<byte[]> GeneratePdfAsync(string reportText, List<ChartDto> charts)
        {
            return await Task.Run(() =>
            {
                // Configure QuestPDF license
                QuestPDF.Settings.License = LicenseType.Community;

                var pdfBytes = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(25);

                        // ---------- Header ----------
                        page.Header()
                            .Text("Game Report")
                            .FontSize(18)
                            .Bold()
                            .AlignCenter();

                        // ---------- Content ----------
                        page.Content().Stack(stack =>
                        {
                            // AI Report Text
                            stack.Item()
                                .PaddingBottom(15)
                                .Text(reportText)
                                .FontSize(12)
                                .LineHeight(1.4f);

                            // Charts
                            foreach (var chart in charts)
                            {
                                stack.Item().Element(chartContainer =>
                                {
                                    chartContainer.Column(col =>
                                    {
                                        col.Item().PaddingBottom(5).Text(chart.Title).FontSize(14).Bold();

                                        switch (chart.Type.ToLower())
                                        {
                                            case "bar":
                                                RenderBarChart(col, chart);
                                                break;
                                            case "pie":
                                                RenderPieChart(col, chart);
                                                break;
                                            case "line":
                                                RenderLineChart(col, chart);
                                                break;
                                        }

                                        col.Item().PaddingVertical(5)
                                            .LineHorizontal(1)
                                            .LineColor(Colors.Grey.Lighten4);
                                    });
                                });
                            }
                        });

                        // ---------- Footer ----------
                        page.Footer()
                            .AlignLeft()
                            .Text($"Generated on {DateTime.UtcNow:yyyy-MM-dd HH:mm}")
                            .FontSize(10);
                    });
                }).GeneratePdf();

                return pdfBytes;
            });
        }

        #region Chart Rendering

        private void RenderBarChart(ColumnDescriptor col, ChartDto chart)
        {
            if (!chart.Values.Any()) return;

            var maxValue = chart.Values.Max();
            var barWidth = 1f / chart.Values.Count;

            col.Item().Row(row =>
            {
                for (int i = 0; i < chart.Values.Count; i++)
                {
                    var value = chart.Values[i];
                    var label = chart.Labels.Count > i ? chart.Labels[i] : $"Item {i + 1}";
                    var barHeight = maxValue > 0 ? (value / maxValue) * 150 : 50;

                    row.RelativeColumn().Element(colEl =>
                    {
                        colEl.Column(colBar =>
                        {
                            colBar.Item()
                                .Text(value.ToString("0.##"))
                                .FontSize(10)
                                .FontColor(Colors.White)
                                .AlignCenter();

                            colBar.Item()
                                .Height((float)barHeight)
                                .Background(Colors.Blue.Medium)
                                .PaddingVertical(2);

                            colBar.Item()
                                .PaddingTop(2)
                                .Text(label)
                                .FontSize(10)
                                .AlignCenter();
                        });
                    });
                }
            });
        }

        private void RenderPieChart(ColumnDescriptor col, ChartDto chart)
        {
            if (!chart.Values.Any()) return;

            var total = chart.Values.Sum();
            
            // Create a simple text-based representation since QuestPDF canvas doesn't support pie charts natively
            col.Item().Column(pieCol =>
            {
                for (int i = 0; i < chart.Values.Count; i++)
                {
                    var label = chart.Labels.Count > i ? chart.Labels[i] : $"Segment {i + 1}";
                    var value = chart.Values[i];
                    var percentage = total > 0 ? (value / total) * 100 : 0;
                    var color = GetChartColor(i);

                    pieCol.Item().Row(row =>
                    {
                        row.ConstantColumn(15).Background(color).Height(15);
                        row.RelativeColumn()
                            .PaddingLeft(10)
                            .Text($"{label}: {value:0.##} ({percentage:0.##}%)")
                            .FontSize(10);
                    });
                }
            });
        }

        private void RenderLineChart(ColumnDescriptor col, ChartDto chart)
        {
            if (!chart.Values.Any()) return;

            var maxValue = chart.Values.Max();
            if (maxValue == 0) maxValue = 1;

            col.Item().Column(lineCol =>
            {
                // Create a text representation of the line chart
                var height = 10;
                for (int level = height; level >= 0; level--)
                {
                    var threshold = maxValue * (level / (float)height);
                    lineCol.Item().Row(row =>
                    {
                        row.ConstantColumn(30).Text($"{(threshold == 0 ? "0" : threshold.ToString("0.#"))}").FontSize(8).AlignRight();
                        row.RelativeColumn().PaddingLeft(5).Element(container =>
                        {
                            var lineText = "";
                            for (int i = 0; i < chart.Values.Count; i++)
                            {
                                lineText += chart.Values[i] >= threshold ? "█ " : "  ";
                            }
                            container.Text(lineText).FontSize(10);
                        });
                    });
                }

                // X-axis labels
                lineCol.Item().Row(row =>
                {
                    row.ConstantColumn(30);
                    row.RelativeColumn().PaddingLeft(5).Element(container =>
                    {
                        var labels = "";
                        for (int i = 0; i < chart.Labels.Count; i++)
                        {
                            labels += (i < chart.Labels.Count ? chart.Labels[i][0].ToString() : (i + 1).ToString()) + " ";
                        }
                        container.Text(labels).FontSize(8);
                    });
                });
            });
        }

        private Color GetChartColor(int index)
        {
            var colors = new[]
            {
                Colors.Blue.Medium,
                Colors.Green.Medium,
                Colors.Red.Medium,
                Colors.Orange.Medium,
                Colors.Purple.Medium,
                Colors.Teal.Medium,
                Colors.Pink.Medium,
                Colors.Amber.Medium
            };
            return colors[index % colors.Length];
        }

        #endregion
    }
}
