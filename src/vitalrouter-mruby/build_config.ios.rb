IOS_VERSION_MIN = '12.0'

MRuby::CrossBuild.new('ios-arm64') do |conf|
  sdk = `xcrun --sdk iphoneos --show-sdk-path`.chomp

  conf.disable_presym

  conf.cc do |cc|
    cc.defines = %w(MRB_WORD_BOXING MRB_NO_STDIO MRB_NO_PRESYM)
    cc.command = 'xcrun'
    cc.flags = %W(-sdk iphoneos clang -arch arm64 -isysroot "#{sdk}" -mios-version-min=#{IOS_VERSION_MIN} -g -Os -Wall -Werror-implicit-function-declaration)
  end

  conf.linker do |linker|
    linker.command = 'xcrun'
    linker.flags = %W(-sdk iphoneos clang -arch arm64 -isysroot "#{sdk}" -mios-version-min=#{IOS_VERSION_MIN})
  end

  conf.gembox '../../../vitalrouter'
end

MRuby::CrossBuild.new('ios-x64') do |conf|
  sdk = `xcrun --sdk iphonesimulator --show-sdk-path`.chomp

  conf.disable_presym
  
  conf.cc do |cc|
    cc.defines = %w(MRB_WORD_BOXING MRB_NO_STDIO MRB_NO_PRESYM)
    cc.command = 'xcrun'
    cc.flags = %W(-sdk iphonesimulator clang -arch x86_64 -isysroot "#{sdk}" -mios-version-min=#{IOS_VERSION_MIN} -g -Os -Wall -Werror-implicit-function-declaration)
  end

  conf.linker do |linker|
    linker.command = 'xcrun'
    linker.flags = %W(-sdk iphonesimulator clang -arch x86_64 -isysroot "#{sdk} -mios-version-min=#{IOS_VERSION_MIN}")
  end

  conf.gembox '../../../vitalrouter'
end
