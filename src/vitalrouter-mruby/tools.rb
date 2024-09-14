require 'fileutils'

PLATFORMS = %w(
  windows-x64
  macOS-arm64
  macOS-x64
  ios-arm64
  ios-x64
  linux-x64
  linux-arm64
  android-x64
  android-arm64
)

def copy_to_unity(build_dir)
  build_dir = File.expand_path(build_dir)
  unity_plugins_dir = File.expand_path('../../VitalRouter.Unity/Assets/VitalRouter.MRuby/Runtime/Plugins', __FILE__)

  Dir.foreach(build_dir) do |dir|
    next unless PLATFORMS.include?(dir)

    ext =
      if dir.start_with?('macOS') ||
         dir.start_with?('macos')
        'dylib'
      elsif dir.start_with?('windows')
        'dll'
      elsif dir.start_with?('ios') ||
            dir.start_with?('wasm')
        'a'
      else
        'so'
      end
    src = File.join(build_dir, dir, 'lib', "libmruby.#{ext}")
    dst = File.join(unity_plugins_dir, dir, "VitalRouter.MRuby.Native.#{ext}")
    FileUtils.cp src, dst, verbose: true
  end
end

case ARGV[0]
when 'copy_to_unity'
  copy_to_unity(ARGV[1])
else
  puts "No such command #{ARGV[0]}"
end
