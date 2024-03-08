<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0"
                xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
                xmlns:xs="http://www.w3.org/2001/XMLSchema"
             xmlns:msb="http://schemas.microsoft.com/developer/msbuild/2003">
  <xsl:output method="text" indent="no"/>
  <xsl:strip-space elements="*"/>
  <xsl:param name="filename"/>
  <xsl:template match="/">
# <xsl:value-of select="$filename"/>
### Properties
<xsl:for-each select="xs:schema/xs:element[@substitutionGroup='msb:Property']">
#### <xsl:value-of select="@name"/>
<xsl:text>&#x0A;&#x0A;</xsl:text>
  <xsl:value-of select="normalize-space(xs:annotation/xs:documentation)"/>
<xsl:text>&#x0A;</xsl:text>
</xsl:for-each>
### Items
<xsl:for-each select="xs:schema/xs:element[@substitutionGroup='msb:Item']">
#### <xsl:value-of select="@name"/>
<xsl:text>&#x0A;&#x0A;</xsl:text>
  <xsl:value-of select="normalize-space(xs:annotation/xs:documentation)"/>
  <xsl:text>&#x0A;</xsl:text>
    </xsl:for-each>
  </xsl:template>
</xsl:stylesheet>
