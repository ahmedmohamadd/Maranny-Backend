SELECT SportName, COUNT(*) Cnt
FROM dbo.Sports
GROUP BY SportName
HAVING COUNT(*) > 1;
