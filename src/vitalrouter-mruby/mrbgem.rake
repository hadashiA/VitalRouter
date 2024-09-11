MRuby::Gem::Specification.new('vitalrouter-mruby') do |spec|
  spec.license = 'MIT'
  spec.authors = 'hadashiA'
end

MRuby.each_target do
  next unless name.match(/^(windows|macOS|android|linux)/i)

  sharedlib_ext =
    if RUBY_PLATFORM.match(/darwin/i)
      'dylib'
    elsif ENV['OS'] == 'Windows_NT'
      'dll'
    else
      'so'
    end
  
  mruby_sharedlib = "#{build_dir}/lib/VitalRouter.MRuby.Native.#{sharedlib_ext}"

  products << mruby_sharedlib

  task mruby_sharedlib => libmruby_static do |t|
    is_vc = primary_toolchain == 'visualcpp'
    is_mingw = ENV['OS'] == 'Windows_NT' && cc.command.start_with?('gcc')

    deffile = "#{File.dirname(__FILE__)}/vitalrouter-mruby.def"

    flags = []
    if is_vc
      flags << '/DLL'
      flags << "/DEF:#{deffile}"
    else
      flags << '-shared'
      flags << '-fpic'
      flags <<
        if sharedlib_ext == 'dylib'
          '-Wl,-force_load'
        elsif is_mingw
          deffile          
        else
          "-Wl,--no-whole-archive"
        end
    end

    flags << "/MACHINE:#{ENV['Platform']}" if is_vc && ENV.include?('Platform')
    flags << libmruby_static
    
    linker.run mruby_sharedlib, [], [], [], flags
  end
end
