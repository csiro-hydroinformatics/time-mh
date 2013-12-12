mLog = read.csv(file='c:/tmp/logMoscem.csv', header=F)
head(moscemLog)
names(moscemLog) <- c('NSE','Bias','Fitness','Generation')
library(ggplot2)
?qplot
qplot( Bias, NSE, data=moscemLog, color=Fitness )
qplot( Bias, NSE, data=moscemLog, color=Fitness, facets = Generation ~ . )
qplot( Bias, NSE, data=moscemLog, color=Generation )
qplot( Bias, NSE, data=moscemLog, color=Generation, xlim = c(0, 0.4), ylim=c(-2, 1))
history()

